// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.EditorTools;

namespace UnityEditor
{
    [CustomEditor(typeof(HingeJoint)), CanEditMultipleObjects]
    class HingeJointEditor : JointEditor<HingeJoint>
    {
        static readonly GUIContent s_WarningMessage = EditorGUIUtility.TrTextContent("Min and max limits must be within the range [-180, 180].");
        SerializedProperty m_MinLimit;
        SerializedProperty m_MaxLimit;

        void OnEnable()
        {
            m_MinLimit = serializedObject.FindProperty("m_Limits.min");
            m_MaxLimit = serializedObject.FindProperty("m_Limits.max");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            float min = m_MinLimit.floatValue;
            float max = m_MaxLimit.floatValue;

            if (min < -180f || min > 180f || max < -180f || max > 180f)
                EditorGUILayout.HelpBox(s_WarningMessage.text, MessageType.Warning);
        }
    }

    [EditorTool("Edit Hinge Joint", typeof(HingeJoint))]
    class HingeJointTool : JointTool<HingeJoint>
    {
        void OnEnable()
        {
            angularLimitHandle.yMotion = ConfigurableJointMotion.Locked;
            angularLimitHandle.zMotion = ConfigurableJointMotion.Locked;

            angularLimitHandle.yHandleColor = Color.clear;
            angularLimitHandle.zHandleColor = Color.clear;

            angularLimitHandle.xRange = new Vector2(-Physics.k_MaxFloatMinusEpsilon, Physics.k_MaxFloatMinusEpsilon);
        }

        protected override void GetActors(
            HingeJoint joint,
            out Transform dynamicPose,
            out Transform connectedPose,
            out int jointFrameActorIndex,
            out bool rightHandedLimit
        )
        {
            base.GetActors(joint, out dynamicPose, out connectedPose, out jointFrameActorIndex, out rightHandedLimit);
            rightHandedLimit = true;
        }

        protected override void DoAngularLimitHandles(HingeJoint joint)
        {
            base.DoAngularLimitHandles(joint);

            angularLimitHandle.xMotion = joint.useLimits ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;

            JointLimits limit;

            limit = joint.limits;
            angularLimitHandle.xMin = limit.min;
            angularLimitHandle.xMax = limit.max;

            EditorGUI.BeginChangeCheck();

            angularLimitHandle.radius = GetAngularLimitHandleSize(Vector3.zero);
            angularLimitHandle.DrawHandle();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(joint, Styles.editAngularLimitsUndoMessage);

                limit = joint.limits;
                limit.min = angularLimitHandle.xMin;
                limit.max = angularLimitHandle.xMax;
                joint.limits = limit;
            }
        }
    }
}
