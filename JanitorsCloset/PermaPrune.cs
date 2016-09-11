﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;


namespace JanitorsCloset
{
    class PermaPruneWindow : MonoBehaviour
    {
        Rect _windowRect = new Rect()
        {
            xMin = Screen.width - 325,
            xMax = Screen.width - 175,
            yMin = Screen.height - 300,
            yMax = 50 //0 height, GUILayout resizes it
        };


        string _windowTitle = string.Empty;

        public static PermaPruneWindow Instance { get; private set; }

        void Awake()
        {
            Log.Info("PermaPruneWindow Awake()");
            this.enabled = false;
            Instance = this;
        }

        void Start()
        {

        }

        void OnEnable()
        {
            Log.Info("PermaPruneWindow OnEnable()");

        }

        public bool isEnabled()
        {
            return this.enabled;
        }

        void CloseWindow()
        {
            this.enabled = false;
            winState = winContent.menu;
            Log.Info("CloseWindow enabled: " + this.enabled.ToString());
        }

        void OnDisable()
        {

        }

        enum winContent
        {
            menu,
            permaprune,
            undo,
            dialog,
            close
        }



        winContent winState = winContent.menu;
        MultiOptionDialog dialog;
        void OnGUI()
        {
            if (isEnabled())
            {                
                switch (winState)
                {
                    case winContent.menu:
                        _windowTitle = string.Format("PermaPrune");
                        var tstyle = new GUIStyle(GUI.skin.window);

                        _windowRect.yMax = _windowRect.yMin;
                        _windowRect = GUILayout.Window(this.GetInstanceID(), _windowRect, WindowContent, _windowTitle, tstyle);
                        break;

                    case winContent.permaprune:
                        dialog = new MultiOptionDialog("This will permanently rename files to prevent them from being loaded", "Permanent Prune", HighLogic.UISkin, new DialogGUIBase[] {
                                                                                     new DialogGUIButton ("OK", () => {
                                                             winState = winContent.close;
                                                             pruner();
                                                            
                                                        }),
                                                        new DialogGUIButton ("Cancel", () => {
                                                            winState = winContent.close;
                                                        })
                                                });
                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dialog, false, HighLogic.UISkin, true);
                        winState = winContent.dialog;
                        break;

                    case winContent.undo:
                        dialog = new MultiOptionDialog("This will permanently rename pruned files to allow them to be loaded", "Unprune (restore)", HighLogic.UISkin, new DialogGUIBase[] {
                                                                                     new DialogGUIButton ("OK", () => {
                                                             unpruner();
                                                             winState = winContent.close;
                                                        }),
                                                        new DialogGUIButton ("Cancel", () => {
                                                            winState = winContent.close;
                                                        })
                                                });
                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dialog, false, HighLogic.UISkin, true);
                        winState = winContent.dialog;
                        break;

                    case winContent.close:
                        CloseWindow();
                        break;
                }

            }
        }

        List<prunedPart> renamedFilesList = null;
        void RenameFile(string path, string name)
        {
            Log.Info("RenameFile, path: " + path + "    name: " + name);
            if (File.Exists(FileOperations.CONFIG_BASE_FOLDER + path))
            {
                if (File.Exists(FileOperations.CONFIG_BASE_FOLDER + path + PRUNED))
                {
                    System.IO.File.Delete(FileOperations.CONFIG_BASE_FOLDER + path + PRUNED);
                }

                Log.Info("Renaming: " + path + "  to  " + path + PRUNED);
                ShowRenamed.Instance.addLine("Renaming: " + path + "  to  " + path + PRUNED);
                System.IO.File.Move(FileOperations.CONFIG_BASE_FOLDER + path, FileOperations.CONFIG_BASE_FOLDER + path + PRUNED);
                prunedPart pp = new prunedPart();
                pp.path = path + PRUNED;
                pp.modName = name;
                renamedFilesList.Add(pp);
            }
        }

        const string PRUNED = ".prune";
        void pruner()
        {
            Log.Info("PermaPrune.pruner");
            renamedFilesList = FileOperations.Instance.loadRenamedFiles();
            Log.Info("sizeof renamedFilesList: " + renamedFilesList.Count.ToString());
            //Log.Info("pruner, sizeof blacklist:" + PartPruner.blackList.Count.ToString());
            ShowRenamed.Instance.Show();

            List<string> prunedParts = new List<string>();
            foreach (blackListPart blp in JanitorsCloset.blackList.Values)
            {
                if (blp.where != blackListType.ALL)
                    continue;
                Log.Info("pruned part: " + blp.modName);
                AvailablePart p = PartLoader.Instance.parts.Find(item => item.name.Equals(blp.modName));
                if (p == null)
                    continue;
                prunedParts.Add(blp.modName);

                //Log.InfoWarning("Part config: " + p.partConfig);

                //Log.Info("Part configFileFullName: " + p.configFileFullName);

                //Log.Info("Part partPath: " + p.partPath);
                //Log.Info("Part partUrl: " + p.partUrl);
                //Log.Info("Part resourceInfo: " + p);
                //Log.Info("Part title: " + p.title);
                //Log.Info("Part internalConfig: " + p.internalConfig);

                // Rename cfg file

                string s1 = p.configFileFullName.Substring(p.configFileFullName.IndexOf("GameData") + 9);

                RenameFile(s1, p.name);

                string partPath = p.partUrl;

                for (int x = 0; x < 1; x++)
                {
                    int backslash = partPath.LastIndexOf('\\');
                    int slash = partPath.LastIndexOf('/');
                    int i = Math.Max(backslash, slash);
                    partPath = partPath.Substring(0, i);
                }
                partPath += "/";

                // rename resource file
                // Look for model =
                //  model has complete path
                // Look for mesh =
                //      with mesh, get patch from cfg file path

                foreach (ConfigNode modelNode in p.partConfig.GetNodes("MODEL"))
                {
                    if (modelNode != null)
                    {
                        string model = modelNode.GetValue("model");

                        // Make sure it isn't being used in another part
                        bool b = false;
                        foreach (AvailablePart pSearch in PartLoader.Instance.parts)
                        {
                            if (p != pSearch)
                            {
                                foreach (ConfigNode searchNode in pSearch.partConfig.GetNodes("MODEL"))
                                {
                                    if (searchNode.GetValue("model") == model)
                                    {
                                        b = true;
                                        break;
                                    }
                                }
                            }
                            if (b)
                                break;
                        }

                        //Log.Info("MODEL: " + model);
                        string mURL = FindTexturePathFromModel.getModelURL(model);
                        //Log.Info("MODEL URL: " + mURL);
                        model = model + ".mu";


                        if (!b)
                        {
                            
                            RenameFile(model, p.name);
                        }
                    }
                }
                string mesh = p.partConfig.GetValue("mesh");
                if (mesh != null && mesh != "")
                {
                    // Make sure it isn't being used in another part
                    bool b = false;
                    foreach (AvailablePart pSearch in PartLoader.Instance.parts)
                    {
                        if (p != pSearch)
                        {
                            string searchMesh = p.partConfig.GetValue("mesh");
                            if (searchMesh == mesh)
                            {
                                b = true;
                                break;
                            }
                            if (b)
                                break;
                        }
                    }
                    if (!b)
                    {
                        //Log.Info("mesh: " + mesh + "    partPath: " + partPath);

                        string mURL = FindTexturePathFromModel.getModelURL(mesh);
                        //Log.Info("mesh partPath 1: " + partPath);
                        //partPath = partPath.Substring(0, partPath.LastIndexOf("/"));
                        //Log.Info("mesh partPath 2: " + partPath);
                        partPath = partPath.Substring(0, partPath.LastIndexOf("/")) + "/";
                        //Log.Info("mesh partPath 3: " + partPath);

                        //Log.Info("mesh URL: " + mURL);
                        if (!(mesh.Contains("/") || mesh.Contains("\\")))
                            mesh = partPath + mesh;
                        //Log.Info("mesh Path: " + mesh);
                        
                        RenameFile(mesh, p.name);
                    }
                }

                // this gets the model
                foreach (ConfigNode internalNode in p.partConfig.GetNodes("INTERNAL"))
                {
                    if (internalNode != null)
                    {
                        UrlDir.UrlConfig config;
                        if (GetInternalSpaceConfigUrl.FindInternalSpaceConfigByName(internalNode.GetValue("name"), out config))
                        {
                            // Make sure it isn't being used in another part
                            bool b = false;
                            foreach (AvailablePart pSearch in PartLoader.Instance.parts)
                            {
                                if (p != pSearch)
                                {
                                    foreach (ConfigNode internalNodeSearch in p.partConfig.GetNodes("INTERNAL"))
                                    {
                                        UrlDir.UrlConfig configSearch;
                                        if (GetInternalSpaceConfigUrl.FindInternalSpaceConfigByName(internalNode.GetValue("name"), out configSearch))
                                        {
                                            if (configSearch.url == config.url)
                                            {
                                                b = true;
                                                break;
                                            }
                                        }
                                        if (b) break;
                                    }
                                }
                                if (b) break;
                            }
                            if (!b)
                            {

                                //Log.Info("config.name: " + config.name);
                                //Log.Info("config.url: " + config.url);
                                string s = config.url.Substring(0, config.url.LastIndexOf("/")) + ".cfg";
                                //Log.Info("Relative path: " + s);
                                RenameFile(s, p.name);
                            }
                        }

                        //
                        // We aren't going to check to see if the different models inside the space are
                        // used elsewhere.  An assumption that the same model won't be used by multiple spaces
                        //
                        ConfigNode cfgNode;
                        bool b1 = GetInternalSpaceConfigUrl.FindInternalSpaceConfigNode(config.name, out cfgNode);
                        if (b1)
                        {
                            //Log.Info("Internal Space Config found: " + config.url);
                            //Log.Info("cfgNode name: " + cfgNode.name );
                            //Log.Info(cfgNode.ToString());
                            foreach (ConfigNode modelNode in cfgNode.GetNodes("MODEL"))
                            {
                                string model = modelNode.GetValue("model");
                                //Log.Info("MODEL: " + model);
                                string mURL = FindTexturePathFromModel.getModelURL(model);
                                // Log.Info("MODEL URL: " + mURL);
                                model = model + ".mu";
                                RenameFile(model, p.name);
                            }

                        }
                    }
                }
            }
            foreach (var s in prunedParts)
            {
                blackListPart blp = JanitorsCloset.blackList[s];
                blp.permapruned = true;
                JanitorsCloset.blackList[s] = blp;
            }

            Log.Info("before saveRenamedFiles");
            FileOperations.Instance.saveRenamedFiles(renamedFilesList);
            //JanitorsCloset.Instance.clearBlackList();
        }

        void unpruner()
        {
            ShowRenamed.Instance.Show();
            renamedFilesList = FileOperations.Instance.loadRenamedFiles();
            foreach (prunedPart l in renamedFilesList)
            {
                l.path = FileOperations.CONFIG_BASE_FOLDER + l.path;
                Log.Info("Renaming " + l.path + "  to  " + l.path.Substring(0, l.path.Length - PRUNED.Length));
                if (File.Exists(l.path))
                {
                    Log.Info("Renaming " + l.path + "  to  " + l.path.Substring(0, l.path.Length - PRUNED.Length));
                    ShowRenamed.Instance.addLine("Renaming " + l.path + "  to  " + l.path.Substring(0, l.path.Length - PRUNED.Length));
                    if (!File.Exists(l.path.Substring(0, l.path.Length - PRUNED.Length)))
                        System.IO.File.Move(l.path, l.path.Substring(0, l.path.Length - PRUNED.Length));
                    else
                        System.IO.File.Delete(l.path);
                }
            }
            FileOperations.Instance.delRenamedFilesList();
        }


        void WindowContent(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Permanent Prune"))
            {
                winState = winContent.permaprune;
                //                        pruner();
                // CloseWindow();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Undo Permanent Prune"))
            {
                winState = winContent.undo;
                //unpruner();
                //CloseWindow();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                CloseWindow();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public void Show()
        {
            Log.Info("PermaPrune Show()");
            this.enabled = true;
        }
    }
}