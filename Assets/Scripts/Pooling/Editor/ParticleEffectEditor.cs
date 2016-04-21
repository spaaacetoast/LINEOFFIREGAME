using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;

[CustomEditor(typeof(ParticleEffect))]
public class ParticleEffectEditor : Editor
{
    AnimBool lightSettingsBool = new AnimBool();
    AnimBool lightSetting1Bool = new AnimBool();
    AnimBool lightSetting2Bool = new AnimBool();
    AnimBool explosionSettingsBool = new AnimBool();
    AnimBool explosionSetting1Bool = new AnimBool();

    public bool hasInit = false;

    public override void OnInspectorGUI()
    {
        if (!hasInit)
        {
            hasInit = true;

            UnityEvent e = new UnityEvent();

            e.AddListener(Repaint);

            lightSettingsBool.valueChanged = e;
            lightSetting1Bool.valueChanged = e;
            explosionSettingsBool.valueChanged = e;
        }

        ParticleEffect pe = target as ParticleEffect;

        EditorGUI.BeginChangeCheck();
        serializedObject.Update();

        pe.particleName = EditorGUILayout.TextField("Particle Name", pe.particleName);
        pe.randomChance = (int)EditorGUILayout.Slider("Random Percentage", pe.randomChance, 0, 100);
        pe.usageTime = EditorGUILayout.FloatField("Particle Time Length", pe.usageTime);
        pe.particleID = EditorGUILayout.IntField("Particle ID", pe.particleID);

        SerializedProperty tps = serializedObject.FindProperty("particleSystems");
        EditorGUILayout.PropertyField(tps, true);
        tps = serializedObject.FindProperty("randomParticleSystems");
        EditorGUILayout.PropertyField(tps, true);

        lightSettingsBool.target = EditorGUILayout.Foldout(lightSettingsBool.target, "Light Settings");

        if (EditorGUILayout.BeginFadeGroup(lightSettingsBool.faded))
        {
            EditorGUI.indentLevel++;
            pe.lightSettings.useLight = EditorGUILayout.ToggleLeft("Use Light", pe.lightSettings.useLight);
            lightSetting2Bool.target = pe.lightSettings.useLight;
            if (EditorGUILayout.BeginFadeGroup(lightSetting2Bool.faded))
            {

                EditorGUI.indentLevel++;
                pe.lightSettings.light = EditorGUILayout.ObjectField("Light Object", pe.lightSettings.light, typeof(Light)) as Light;
                pe.lightSettings.lightDisableTime = EditorGUILayout.FloatField("Disable Length", pe.lightSettings.lightDisableTime);

                pe.lightSettings.smoothDisable = EditorGUILayout.ToggleLeft("Smooth Disable", pe.lightSettings.smoothDisable);
                lightSetting1Bool.target = pe.lightSettings.smoothDisable;
                if (EditorGUILayout.BeginFadeGroup(lightSetting1Bool.faded))
                {
                    EditorGUI.indentLevel++;
                    pe.lightSettings.disableSpeed = EditorGUILayout.FloatField("Smooth Disable Length", pe.lightSettings.disableSpeed);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFadeGroup();

        explosionSettingsBool.target = EditorGUILayout.Foldout(explosionSettingsBool.target, "Explosion Settings");

        if (EditorGUILayout.BeginFadeGroup(explosionSettingsBool.faded))
        {
            EditorGUI.indentLevel++;
            explosionSetting1Bool.target = EditorGUILayout.ToggleLeft("Is Explosion", explosionSetting1Bool.target);
            pe.explosionSettings.isExplosion = explosionSetting1Bool.target;
            if (EditorGUILayout.BeginFadeGroup(explosionSetting1Bool.faded))
            {
                pe.explosionSettings.explosionMinRange = EditorGUILayout.FloatField("Minimum Effect Range", pe.explosionSettings.explosionMinRange);
                pe.explosionSettings.explosionMaxRange = EditorGUILayout.FloatField("Maximum Effect Range", pe.explosionSettings.explosionMaxRange);
                pe.explosionSettings.explosionStrength = EditorGUILayout.FloatField("Force Strength", pe.explosionSettings.explosionStrength);
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFadeGroup();

        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();
        EditorGUIUtility.LookLikeControls();

        Repaint();
    }
}
