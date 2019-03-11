/*              #########                       
              ############                     
              #############                    
             ##  ###########                   
            ###  ###### #####                  
            ### #######   ####                 
           ###  ########## ####                
          ####  ########### ####               
         ####   ###########  #####             
        #####   ### ########   #####           
       #####   ###   ########   ######         
      ######   ###  ###########   ######       
     ######   #### ##############  ######      
    #######  #####################  ######     
    #######  ######################  ######    
   #######  ###### #################  ######   
   #######  ###### ###### #########   ######   
   #######    ##  ######   ######     ######   
   #######        ######    #####     #####    
    ######        #####     #####     ####     
     #####        ####      #####     ###      
      #####       ###        ###      #        
        ###       ###        ###              
         ##       ###        ###               
__________#_______####_______####______________
    身是菩提树，心如明镜台，时时勤拂拭，勿使惹尘埃。
                我们的未来没有BUG              
* ==============================================================================
* Filename: BundleEditor 
* Created:  
* Author:   WYC
* Purpose:  打包加载 AssetBundle
* ==============================================================================
*/
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class BundleEditor
{
    private static string m_BunleTargetPath = Application.streamingAssetsPath;
    private static string ABCONGFIGPATH = "Assets/Editor/ABConfig.asset";
    //key是AB包名，value是路径，所有文件AB包dic
    private static Dictionary<string, string> m_AllFileDir = new Dictionary<string, string>();
    //过滤的list
    private static List<string> m_AllFileAB = new List<string>();
    //单个Prefab的AB包
    private static Dictionary<string, List<string>> m_AllPrefabDir = new Dictionary<string, List<string>>();
    //存储所有有效路径
    private static List<string> m_ConfigFile = new List<string>();
    //标签
    [MenuItem("Tools/AB打包")]
    public static void Build()
    {
        //AssetBundle打包设置
        m_AllFileAB.Clear();
        m_AllFileDir.Clear();
        m_AllPrefabDir.Clear();
        m_ConfigFile.Clear();
        ABConfig abconfig = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONGFIGPATH);
        foreach (ABConfig.FileDirABName f in abconfig.m_AllFileDirAB)
        {
            if (m_AllFileDir.ContainsKey(f.ABName))
            {
                Debug.LogError("AB包配置名字重复，请检查！");
            }
            else
            {
                m_AllFileDir.Add(f.ABName, f.Path);
                m_AllFileAB.Add(f.Path);
                m_ConfigFile.Add(f.Path);
            }
        }
        //string[] allStr = AssetDatabase.FindAssets("t:Prefab", abconfig.m_AllPrefabPath.ToArray());//加载预设体
        //for (int i = 0; i < allStr.Length; i++)
        //{
        //    string path = AssetDatabase.GUIDToAssetPath(allStr[i]);
        //    //加载进度条
        //    EditorUtility.DisplayProgressBar("查找Prefab", "Prefab" + path, i * 1.0f / allStr.Length);
        //    m_ConfigFile.Add(path);
        //    if (!ContainAllFileAB(path))
        //    {
        //        GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        //        string[] allDepend = AssetDatabase.GetDependencies(path);
        //        List<string> allDependPath = new List<string>();
        //        for (int j = 0; j < allDepend.Length; j++)
        //        {
        //            //Debug.Log(allDepend[j]);
        //            if (!ContainAllFileAB(allDepend[j]) && !allDepend[j].EndsWith(".cs") && !allDepend[j].EndsWith(".prefab"))
        //            {
        //                m_AllFileAB.Add(allDepend[j]);
        //                allDependPath.Add(allDepend[j]);
        //            }
        //        }
        //        if (m_AllPrefabDir.ContainsKey(obj.name))
        //        {
        //            Debug.LogError("存在相同名字的Prefab名字" + obj.name);
        //        }
        //        else
        //        {
        //            m_AllPrefabDir.Add(obj.name, allDependPath);
        //        }
        //    }
        //}
        foreach (string name in m_AllFileDir.Keys)
        {
            SetABName(name, m_AllFileDir[name]);
        }
        foreach (string name in m_AllPrefabDir.Keys)
        {
            SetABName(name, m_AllPrefabDir[name]);
        }

        BunildAssetBunble();

        //清除老的AB包设置
        string[] oldNameAB = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < oldNameAB.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldNameAB[i], true);
            EditorUtility.DisplayProgressBar("清除AB包名", "名字" + oldNameAB[i], i * 1.0f / oldNameAB.Length);
        }

        AssetDatabase.Refresh();//编辑器的刷新
        EditorUtility.ClearProgressBar();//清除进度条

    }



    //开始AB打包
    static void BunildAssetBunble()
    {
        string[] allBundles = AssetDatabase.GetAllAssetBundleNames();
        //key 为全路径 value为包名
        Dictionary<string, string> resPathDic = new Dictionary<string, string>();
        for (int i = 0; i < allBundles.Length; i++)
        {
            string[] allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(allBundles[i]);
            for (int j = 0; j < allBundlePath.Length; j++)
            {
                if (allBundlePath[j].EndsWith(".cs")|| allBundlePath[j].EndsWith(".prefab"))
                {
                    continue;
                }
                Debug.Log("此AB包：" + allBundles[i] + "下面包含的资源文件路径：" + allBundlePath[j]);
                if (ValidPath(allBundlePath[j]))
                {
                    resPathDic.Add(allBundlePath[j], allBundles[i]);
                }
            }
        }

        DeleteAB();
        WriteDate(resPathDic);

        BuildPipeline.BuildAssetBundles(m_BunleTargetPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.Android);
    }
    //生成自己的AB配置表
    static void WriteDate(Dictionary<string, string> resPathDic)
    {
        AssetBundleConfig config = new AssetBundleConfig();
        config.ABList = new List<ABBase>();
        foreach (string s in resPathDic.Keys)
        {
            ABBase aBBase = new ABBase();
            aBBase.Path = s;
            aBBase.Crc = CRC32.GetCRC32(s);
            aBBase.ABName = resPathDic[s];
            aBBase.AssetName = s.Remove(0, s.LastIndexOf("/") + 1);
            aBBase.ABDependce = new List<string>();
            string[] resDependce = AssetDatabase.GetDependencies(s);
            for (int i = 0; i < resDependce.Length; i++)
            {
                string tempPath = resDependce[i];
                if (tempPath == s || s.EndsWith(".cs"))
                {
                    continue;
                }
                string abName = "";
                if (resPathDic.TryGetValue(tempPath, out abName))
                {
                    if (abName == resPathDic[s])
                    {
                        continue;
                    }
                    if (!aBBase.ABDependce.Contains(abName))
                    {
                        aBBase.ABDependce.Add(abName);
                    }
                }
            }
            config.ABList.Add(aBBase);
        }
        //写入XML
        string xmlPath = Application.dataPath + "/Resources/XML/AssetBundleConfig.xml";
        if (File.Exists(xmlPath))
        {
            File.Delete(xmlPath);
        }
        using (FileStream fileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            using (StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.UTF8))
            {
                XmlSerializer xml = new XmlSerializer(config.GetType());
                xml.Serialize(sw, config);
            }
        }
        //写入二进制
        foreach (ABBase a in config.ABList)
        {
            a.Path = "";
        }
        string batysPath = Application.dataPath + "/Resources/Bytes/AssetBundleConfig.bytes";
        if (File.Exists(batysPath))
        {
            File.Delete(batysPath);
        }
        using (FileStream fileStream = new FileStream(batysPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fileStream, config);
        }
    }

    //删除多余的AB包资源
    static void DeleteAB()
    {
        string[] allBundlesName = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo info = new DirectoryInfo(m_BunleTargetPath);
        FileInfo[] files = info.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (ConatinABName(files[i].Name, allBundlesName) || files[i].Name.EndsWith(".meta"))
            {
                continue;
            }
            else
            {
                Debug.Log("此AB包已经改名字了： " + files[i].Name);
                if (File.Exists(files[i].FullName))
                {
                    File.Delete(files[i].FullName);
                }
            }
        }
    }
    static bool ConatinABName(string name, string[] strs)
    {
        for (int i = 0; i < strs.Length; i++)
        {
            if (name == strs[i])
            {
                return true;
            }
        }
        return false;
    }

    //设置AB包名字
    static void SetABName(string name, string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if (assetImporter == null)
        {
            Debug.LogError("不存在的路径文件" + path);
        }
        else
        {
            assetImporter.assetBundleName = name;
        }
    }
    static void SetABName(string name, List<string> paths)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            SetABName(name, paths[i]);
        }
    }

    //过滤的list  有没有重复的路径
    static bool ContainAllFileAB(string path)
    {
        for (int i = 0; i < m_AllFileAB.Count; i++)
        {
            if (path == m_AllFileAB[i] || (path.Contains(m_AllFileAB[i]) && (path.Replace(m_AllFileAB[i], "")[0] == '/')))
            {
                return true;
            }
        }
        return false;
    }

    //是否有效路径
    static bool ValidPath(string path)
    {
        for (int i = 0; i < m_ConfigFile.Count; i++)
        {
            if (path.Contains(m_ConfigFile[i]))
            {
                return true;
            }
        }
        return false;
    }
}