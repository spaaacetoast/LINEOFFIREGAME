using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.Collections;
using UnityEngine.Events;

[CustomEditor(typeof(FadeEffect))]
public class FadeEffectEditor : Editor
{
    AnimBool ImageBasedBool = new AnimBool();
    AnimBool FadeBool = new AnimBool();
    AnimBool RotationBool = new AnimBool();
    AnimBool PositionBool = new AnimBool();

    bool hasInit = false;

    public override void OnInspectorGUI()
    {
        if (!hasInit)
        {
            UnityEvent e = new UnityEvent();

            e.AddListener(Repaint);

            ImageBasedBool.valueChanged = e;
            FadeBool.valueChanged = e;
            RotationBool.valueChanged = e;
            PositionBool.valueChanged = e;
        }

        EditorGUI.BeginChangeCheck();
        serializedObject.Update();

        FadeEffect fe = target as FadeEffect;

        fe.animationType = (FadeEffect.AnimationType)EditorGUILayout.EnumPopup("Animation Type", fe.animationType);

        ImageBasedBool.target = fe.animationType == FadeEffect.AnimationType.ImageBased;
        FadeBool.target = fe.animationType == FadeEffect.AnimationType.Fade;
        RotationBool.target = fe.animationType == FadeEffect.AnimationType.Rotation;
        PositionBool.target = fe.animationType == FadeEffect.AnimationType.Position;

        fe.beginOnAwake = EditorGUILayout.Toggle("Play on Start", fe.beginOnAwake);
        fe.resetObject = EditorGUILayout.Toggle("Apply values on Start", fe.resetObject);
        fe.animationSpeed = EditorGUILayout.FloatField("Animation Speed", fe.animationSpeed);
        fe.animationDelay = EditorGUILayout.FloatField("Animation Delay", fe.animationDelay);

        GUILayout.Space(5);

        if(EditorGUILayout.BeginFadeGroup(ImageBasedBool.faded))
        {
            fe.animationSpeed = EditorGUILayout.FloatField("Fade Speed", fe.animationSpeed);

            SerializedProperty tps = serializedObject.FindProperty("spriteInfo");
            EditorGUILayout.PropertyField(tps, true);
        }
        EditorGUILayout.EndFadeGroup();
        if (EditorGUILayout.BeginFadeGroup(FadeBool.faded))
        {
            fe.fromColor = EditorGUILayout.ColorField("From Color", fe.fromColor);
            fe.targetColor = EditorGUILayout.ColorField("Target Color", fe.targetColor);
        }
        EditorGUILayout.EndFadeGroup();
        if (EditorGUILayout.BeginFadeGroup(RotationBool.faded))
        {
            fe.fromRotation = EditorGUILayout.Vector3Field("From Rotation", fe.fromRotation);
        }
        EditorGUILayout.EndFadeGroup();
        if (EditorGUILayout.BeginFadeGroup(PositionBool.faded))
        {
            fe.fromPosition = EditorGUILayout.Vector3Field("From Position", fe.fromPosition);
        }
        EditorGUILayout.EndFadeGroup();

        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();
        EditorGUIUtility.LookLikeControls();
    }
}
