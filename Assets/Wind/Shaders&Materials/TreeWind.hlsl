#ifndef TREE_WIND_INCLUDED
#define TREE_WIND_INCLUDED

// Stateless object-space deformation. No textures, time, spring state or scene references.
// DirectionOS selects the horizontal tilt direction; Strength is signed -1..1; negative values reverse the bend angle.
float3 TreeWindRotate(float3 value, float3 axis, float angle)
{
    float sine, cosine;
    sincos(angle, sine, cosine);
    return value * cosine + cross(axis, value) * sine
         + axis * dot(axis, value) * (1.0 - cosine);
}

// Constant-curvature centreline parameterized by original height (arc length).
// The lowest ring has zero rotation, so all four bottom vertices stay fixed.
void TreeWindTrunkFrame(float3 pointOS, float3 rootOS, float height,
    float3 directionOS, float3 axis, float tipAngle,
    out float3 bentPointOS, out float localAngle, out float axialScale)
{
    float3 relative = pointOS - rootOS;
    float y = clamp(relative.y, 0.0, height);
    localAngle = tipAngle * (y / height);
    float sine, cosine;
    sincos(localAngle, sine, cosine);
    // sinc/cosc limits avoid cancellation and division by zero near rest.
    float squared = localAngle * localAngle;
    float sinc = abs(localAngle) < 0.01
        ? 1.0 - squared / 6.0 + squared * squared / 120.0
        : sine / localAngle;
    float cosc = abs(localAngle) < 0.01
        ? localAngle * (0.5 - squared / 24.0 + squared * squared / 720.0)
        : (1.0 - cosine) / localAngle;
    float3 centre = rootOS + y * (float3(0, 1, 0) * sinc + directionOS * cosc);
    float3 offset = float3(relative.x, relative.y - y, relative.z);
    bentPointOS = centre + TreeWindRotate(offset, axis, localAngle);
    float curvature = (relative.y >= 0.0 && relative.y <= height) ? tipAngle / height : 0.0;
    axialScale = 1.0 - curvature * dot(float3(relative.x, 0, relative.z), directionOS);
}

// Core evaluation: absolute object-space position and normal, independent of ASE vertex mode.
// PivotOS and BranchWeight come from the existing vertex Color decoding.
// Current mesh: trunk Weight=0, both rigid child islands Weight=1.
void TreeWindEvaluate(
    float3 PositionOS, float3 NormalOS, float3 DirectionOS, float Strength,
    float3 RootOS, float TrunkHeight, float3 PivotOS, float BranchWeight,
    float TrunkMaxAngleDegrees, float BranchMaxAngleDegrees,
    out float3 BentPositionOS, out float3 BentNormalOS)
{
    BentPositionOS = PositionOS;
    BentNormalOS = NormalOS;
    float3 direction = float3(DirectionOS.x, 0, DirectionOS.z);
    float lengthSquared = dot(direction, direction);
    float strength = clamp(Strength, -1.0, 1.0);
    if (lengthSquared < 1e-12 || abs(strength) < 1e-7 || TrunkHeight <= 1e-4)
        return;
    direction *= rsqrt(lengthSquared);
    float3 axis = cross(float3(0, 1, 0), direction);
    float tipAngle = radians(clamp(TrunkMaxAngleDegrees, 0.0, 100.0)) * strength;
    float childAngle = radians(clamp(BranchMaxAngleDegrees, 0.0, 75.0)) * strength;
    float weight = saturate(BranchWeight);
    float3 trunkPosition, trunkNormal;
    float angle, axialScale;
    TreeWindTrunkFrame(PositionOS, RootOS, TrunkHeight, direction, axis, tipAngle,
        trunkPosition, angle, axialScale);
    // Inverse transpose of the bend Jacobian: correct the axial normal component
    // before rotating the cross-section. Very thick/tightly curved trunks are unsupported.
    trunkNormal = TreeWindRotate(float3(NormalOS.x, NormalOS.y / max(axialScale, 0.05), NormalOS.z), axis, angle);
    if (weight <= 0.0)
    {
        BentPositionOS = trunkPosition;
        BentNormalOS = normalize(trunkNormal);
        return;
    }
    float3 bentPivot;
    TreeWindTrunkFrame(PivotOS, RootOS, TrunkHeight, direction, axis, tipAngle,
        bentPivot, angle, axialScale);
    // All eight child vertices share ONE attachment frame. Do not bend them by
    // their individual heights: that would shear/stretch the rigid branch.
    float3 childOffset = TreeWindRotate(PositionOS - PivotOS, axis, childAngle);
    float3 childPosition = bentPivot + TreeWindRotate(childOffset, axis, angle);
    float3 childNormal = TreeWindRotate(TreeWindRotate(NormalOS, axis, childAngle), axis, angle);
    BentPositionOS = lerp(trunkPosition, childPosition, weight);
    BentNormalOS = normalize(lerp(trunkNormal, childNormal, weight));
}
// ASE File Custom Expression entry. Output Type = Float3, Precision = Float,
// Precision suffix = OFF. One Float3 return value only; no Out/InOut ports.
// DirectionWS is a world-space direction; the core above explicitly takes DirectionOS.
// Authored mesh root must be at object-space (0, 0, 0).
// ASE owns ASE_VERSION / ASE_ABSOLUTE_VERTEX_POS. Do not define them in this file.
float3 TreeWind(
    float3 PositionOS, float3 DirectionWS, float Strength,
    float BranchWeight, float3 PivotOS, float TrunkHeight,
    float TrunkMaxAngleDegrees, float BranchMaxAngleDegrees)
{
    float3 bentPositionOS, unusedNormalOS;
    float3 directionOS = mul((float3x3)UNITY_MATRIX_I_M, DirectionWS);
    TreeWindEvaluate(PositionOS, float3(0, 1, 0), directionOS, Strength,
        float3(0, 0, 0), TrunkHeight, PivotOS, BranchWeight,
        TrunkMaxAngleDegrees, BranchMaxAngleDegrees, bentPositionOS, unusedNormalOS);
#if defined(ASE_VERSION) && !defined(ASE_ABSOLUTE_VERTEX_POS)
    // ASE Vertex Offset mode: the generated shader adds this to PositionOS.
    return bentPositionOS - PositionOS;
#else
    // ASE Absolute Vertex Position mode (or a direct non-ASE call).
    return bentPositionOS;
#endif
}
#endif
