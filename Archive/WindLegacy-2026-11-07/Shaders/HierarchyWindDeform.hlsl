#ifndef HIERARCHY_WIND_DEFORM_INCLUDED
#define HIERARCHY_WIND_DEFORM_INCLUDED
TEXTURE2D(_HWData);
SAMPLER(sampler_HWData);
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
float4 _HWRoot;
float4 _HWBakeMin;
float4 _HWBakeSize;
CBUFFER_END
UNITY_INSTANCING_BUFFER_START(HWInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4, _HWResponse)
    UNITY_DEFINE_INSTANCED_PROP(float4, _HWDelayed)
    UNITY_DEFINE_INSTANCED_PROP(float4, _HWAngles)
UNITY_INSTANCING_BUFFER_END(HWInstance)

float3 HWRotate(float3 v, float3 axis, float angle)
{
    float s, c;
    sincos(angle, s, c);
    return v*c + cross(axis,v)*s + axis*dot(axis,v)*(1-c);
}

void HWAxisAngle(float2 response, float degrees, out float3 axis, out float angle)
{
    float magnitude = length(response);
    float3 worldDirection = float3(response.x, 0, response.y) / max(magnitude, 1e-6);
    // Rotation and uniform scaling of the instance are supported. Avoid nonuniform scale.
    float3 directionOS = TransformWorldToObjectDir(worldDirection, false);
    directionOS.y = 0;
    float len = length(directionOS);
    axis = len > 1e-6 ? cross(float3(0,1,0), directionOS/len) : float3(0,0,1);
    angle = len > 1e-6 ? radians(clamp(degrees*magnitude, -75.0, 75.0)) : 0;
}

void HWDeform(inout float3 positionOS, inout float3 normalOS, float4 color, uint vertexID)
{
    float4 pivotWeight = float4(color.rgb * _HWBakeSize.xyz + _HWBakeMin.xyz, color.a);
    float3 tip = LOAD_TEXTURE2D_LOD(_HWData, int2(0,vertexID), 0).xyz
        * _HWBakeSize.xyz + _HWBakeMin.xyz;
    float4 angles = UNITY_ACCESS_INSTANCED_PROP(HWInstance, _HWAngles);
    float2 response = UNITY_ACCESS_INSTANCED_PROP(HWInstance, _HWResponse).xy;
    float2 delayed = UNITY_ACCESS_INSTANCED_PROP(HWInstance, _HWDelayed).xy;
    float3 axis;
    float angle;
    // Each child rotates as one rigid island, then inherits the CURRENT parent transform.
    // Delay changes its local rotation only, never the parent connection position.
    float branchSeed = dot(tip-pivotWeight.xyz, float3(1.31,2.17,3.73));
    float variation = 1 + angles.z * sin(branchSeed * 4.71);
    HWAxisAngle(delayed, angles.y * variation * saturate(pivotWeight.w), axis, angle);
    positionOS = pivotWeight.xyz + HWRotate(positionOS-pivotWeight.xyz, axis, angle);
    normalOS = HWRotate(normalOS, axis, angle);
    HWAxisAngle(response, angles.x, axis, angle);
    positionOS = _HWRoot.xyz + HWRotate(positionOS-_HWRoot.xyz, axis, angle);
    normalOS = HWRotate(normalOS, axis, angle);
}
#endif
