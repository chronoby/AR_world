using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PrefabChooseController : MonoBehaviour
{
    public void ButtonRobotOnClick()
    {
        GoogleARCore.ARWorld.ARWorldController.GameObjectIndex = 0;
    }

    public void ButtonFoxOnClick()
    {
        GoogleARCore.ARWorld.ARWorldController.GameObjectIndex = 1;
    }
    
    public void ButtonCatOnClick()
    {
        GoogleARCore.ARWorld.ARWorldController.GameObjectIndex = 2;
    }

    public void ButtonWindowOnClick()
    {
        GoogleARCore.ARWorld.ARWorldController.GameObjectIndex = 3;
    }

    public void Back()
    {
        SceneManager.LoadScene(0);
    }
}
