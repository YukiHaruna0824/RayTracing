using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleFileBrowser;

public class UIManager : MonoBehaviour
{
    private string _rootFolder;
    private Parser _parser = new Parser();

    public void OpenFileBrowser()
    {
        FileBrowser.AddQuickLink("Desktop", "C:\\Users\\Desktop", null);
        StartCoroutine(ShowLoadDialogCorotine());
    }


    IEnumerator ShowLoadDialogCorotine()
    {
        yield return FileBrowser.WaitForLoadDialog(false, null, "Select Folder", "Select");
        _rootFolder = FileBrowser.Result;
        _parser.Parse(_rootFolder);
    }
}
