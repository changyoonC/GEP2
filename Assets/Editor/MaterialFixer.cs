using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MaterialFixer : EditorWindow
{
    [MenuItem("Tools/Fix Pink Materials")]
    public static void ShowWindow()
    {
        GetWindow<MaterialFixer>("Material Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Material Fixer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Fix All Pink Materials"))
        {
            FixAllPinkMaterials();
        }

        GUILayout.Space(10);
        
        if (GUILayout.Button("Convert to URP Materials"))
        {
            ConvertToURPMaterials();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Convert to HDRP Materials"))
        {
            ConvertToHDRPMaterials();
        }
    }

    private void FixAllPinkMaterials()
    {
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        int fixedCount = 0;

        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null && IsPinkMaterial(material))
            {
                FixMaterial(material);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Fixed {fixedCount} pink materials");
    }

    private bool IsPinkMaterial(Material material)
    {
        return material.shader.name.Contains("Hidden/InternalErrorShader") || 
               material.shader == null ||
               material.shader.name.Contains("Missing");
    }

    private void FixMaterial(Material material)
    {
        // 기본 Standard 셰이더로 교체
        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null)
        {
            material.shader = standardShader;
        }
        else
        {
            // URP나 HDRP 환경에서는 각각의 기본 셰이더 사용
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader != null)
            {
                material.shader = litShader;
            }
            else
            {
                Shader hdrpLitShader = Shader.Find("HDRP/Lit");
                if (hdrpLitShader != null)
                {
                    material.shader = hdrpLitShader;
                }
            }
        }

        EditorUtility.SetDirty(material);
    }

    private void ConvertToURPMaterials()
    {
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        int convertedCount = 0;

        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
            {
                if (material.shader.name.Contains("Standard"))
                {
                    Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpLitShader != null)
                    {
                        material.shader = urpLitShader;
                        convertedCount++;
                        EditorUtility.SetDirty(material);
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Converted {convertedCount} materials to URP");
    }

    private void ConvertToHDRPMaterials()
    {
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        int convertedCount = 0;

        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
            {
                if (material.shader.name.Contains("Standard"))
                {
                    Shader hdrpLitShader = Shader.Find("HDRP/Lit");
                    if (hdrpLitShader != null)
                    {
                        material.shader = hdrpLitShader;
                        convertedCount++;
                        EditorUtility.SetDirty(material);
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Converted {convertedCount} materials to HDRP");
    }
}