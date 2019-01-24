//MIT License

//Copyright(c) 2019 Tadej Gregorcic

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using UnityEngine;
using UnityEditor;

public class AnimatorDuplicator : EditorWindow
{
    protected static UnityEditor.Animations.AnimatorController selectedAnimator = null;
    protected static string targetAnimatorName = null;
    protected static float percentage = 0f;
    protected static int currentAnim = 0, totalAnims = 0;
    protected static bool duplicating = false;

    [SerializeField]
    protected static string targetFolder = "Animations/Duplicate Animations";

    protected const string editorWindowSettingsName = "Motiviti.AnimatorDuplicator";

    private void OnEnable()
    {
        var data = EditorPrefs.GetString(editorWindowSettingsName, JsonUtility.ToJson(this, false));
        JsonUtility.FromJsonOverwrite(data, this);
    }

    protected void OnDisable()
    {
        var data = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString(editorWindowSettingsName, data);
    }

    [MenuItem("Assets/Motiviti Tools/Duplicate Animator and Animations")]
    private static void LoadAdditiveScene()
    {
        if (Selection.objects.Length != 1)
        {
            EditorUtility.DisplayDialog("Alert", "Select only one object.", "OK");
            return;
        }

        var obj = Selection.objects[0];
        var type = obj != null ? obj.GetType().ToString() : "null";

        if (!(Selection.objects[0] is UnityEditor.Animations.AnimatorController))
        {
            EditorUtility.DisplayDialog("Alert", "Select an animator object, not: " + type, "OK");
            return;
        }

        selectedAnimator = Selection.GetFiltered<UnityEditor.Animations.AnimatorController>(SelectionMode.Assets)[0];

        targetAnimatorName = selectedAnimator.name + "_duplicate";

        var window = GetWindow<AnimatorDuplicator>();
        window.position = new Rect(Screen.width * 0.5f, Screen.height * 0.5f, 480, 160);
        window.titleContent.text = "Duplicate";
        window.Show();
    }

    private void OnGUI()
    {
        if (selectedAnimator != null && selectedAnimator is UnityEditor.Animations.AnimatorController)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("ANIMATOR DUPLICATOR", EditorStyles.boldLabel);
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("The animator will be duplicated along with all its corresponding animations.");
            EditorGUILayout.Separator();

            var cntLayers = selectedAnimator.layers.Length;
            var cntAnimations = selectedAnimator.animationClips.Length;

            EditorGUILayout.LabelField(selectedAnimator.name + " (layers: " + cntLayers + ")", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target animator name: ");
            targetAnimatorName = EditorGUILayout.TextField(targetAnimatorName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target folder inside /Assets: ");
            targetFolder = EditorGUILayout.TextField(targetFolder);

            if(GUILayout.Button("Find folder"))
            {
                var selectedFolder = EditorUtility.OpenFolderPanel("Target folder", "", "");

                if(selectedFolder.Contains("Assets"))
                {
                    targetFolder = selectedFolder.Substring(selectedFolder.IndexOf("Assets/")+7);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            if (GUILayout.Button("Duplicate animator"))
            {
                DuplicateAnimator(selectedAnimator, targetAnimatorName);
            }

            if(duplicating)
            {
                EditorGUILayout.LabelField("Duplicating ...");
            }
            else if(currentAnim > 0)
            {
                EditorGUILayout.LabelField("Done: " + totalAnims.ToString() + " animation states.");
            }
        }
        else
        {
            EditorGUILayout.LabelField("Error loading animator.");
        }
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    private string CleanUpFolderName(string folder)
    {
        folder = folder.Replace('\\', '/');
        folder = folder.Trim('/');
        return folder;
    }

    private void GetFolderAndParentPath(string folder, out string outFolder, out string outPath)
    {
        if(String.IsNullOrEmpty(folder))
        {
            outFolder = outPath = null;
        }

        folder = CleanUpFolderName(folder);

        if (folder.Contains("/"))
        {
            var pos = folder.LastIndexOf('/');

            outFolder = folder.Substring(pos+1);
            outPath = folder.Substring(0, pos);
        }
        else
        {
            outFolder = folder;
            outPath = null;
        }
    }

    private string ProcessFolder(string folder)
    {
        var path = "";

        if (String.IsNullOrEmpty(folder))
        {
            Debug.LogError("Target folder name empty.");
            return null;
        }

        GetFolderAndParentPath(folder, out folder, out path);

        return ProcessFolder(folder, path);
    }

    private string ProcessFolder(string folder, string path)
    {
        if(path != null)
        {
            string f1, p1;
            GetFolderAndParentPath(path, out f1, out p1);
            ProcessFolder(f1, p1);
        }

        var parentPath = "Assets" + (path != null ? "/" + path : "");
        var guid = "";
        var fullPath = parentPath + "/" + folder;
        string retFolder = null;

        if (AssetDatabase.IsValidFolder(fullPath))
        {
            return fullPath;
        }
        else
        {
            try
            {
                guid = AssetDatabase.CreateFolder(parentPath, folder);
                retFolder = AssetDatabase.GUIDToAssetPath(guid);
            }
            catch (Exception e)
            {
                Debug.LogError("ERROR CREATING FOLDER; " + e + " " + parentPath + ", " + folder);
            }
        }

        return retFolder;
    }

    private string TrimStringBeginning(string haystack, string needle)
    {
        int i = haystack.IndexOf(needle);
        if(i == 0)
        {
            return haystack.Substring(needle.Length);
        }
        return haystack;
    }

    private void DuplicateAnimator(UnityEditor.Animations.AnimatorController sourceAnimator, string targetName)
    {
        var folder = AnimatorDuplicator.targetFolder;

        folder = TrimStringBeginning(folder, "Assets");

        duplicating = true;

        totalAnims = 0;
        currentAnim = 0;

        foreach (var layer in sourceAnimator.layers)
        {
            totalAnims += layer.stateMachine.states.Length;
        }

        folder = ProcessFolder(folder);
        var animFolderStr = folder.Replace("Assets/", "") + "/" + targetName + " Animations";
        var animFolder = ProcessFolder(animFolderStr);

        // Duplicate the Animator Controller first
        var newAnimatorPath = folder + "/" + targetName + ".controller";
        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(sourceAnimator), newAnimatorPath);

        UnityEditor.Animations.AnimatorController newAnimator = AssetDatabase.LoadAssetAtPath(newAnimatorPath, typeof(UnityEditor.Animations.AnimatorController)) as UnityEditor.Animations.AnimatorController;

        // Iterate through the animation clips on all the layers 
        ProcessAnimationLayers(animFolderStr, newAnimator);

        duplicating = false;
    }

    private void ProcessAnimationLayers(string animFolderStr, UnityEditor.Animations.AnimatorController newAnimator)
    {
        foreach (var layer in newAnimator.layers)
        {
            ProcessAnimationLayer(animFolderStr, layer.name, layer.stateMachine);
        }
    }

    private void ProcessAnimationLayer(string animFolderStr, string layer, UnityEditor.Animations.AnimatorStateMachine parentMachine)
    {
        foreach (var state in parentMachine.states)
        {
            ProcessAnimationState(animFolderStr, layer, parentMachine.name, state);
        }

        foreach (var machine in parentMachine.stateMachines)
        {
            ProcessAnimationLayer(animFolderStr, layer, machine.stateMachine);
        }
    }

    private void ProcessAnimationState(string animFolderStr, string layer, string stateMachine, UnityEditor.Animations.ChildAnimatorState state)
    {
        var originalAnimation = state.state.motion as AnimationClip;

        // Some states have no animation clips attached
        if (originalAnimation != null)
        {
            AnimationClip newAnimation = null;

            try
            {
                newAnimation = Instantiate(originalAnimation);
                AssetDatabase.CreateAsset(newAnimation, ProcessFolder(animFolderStr + "/" + layer + "/" + stateMachine) + "/" + originalAnimation.name + ".anim");
                state.state.motion = newAnimation;
                currentAnim++;
            }
            catch (Exception e)
            {
                Debug.LogError("Could not duplicate animation clip " + state.state.ToString() + " // " + e.Message);
            }
        }
    }
}

