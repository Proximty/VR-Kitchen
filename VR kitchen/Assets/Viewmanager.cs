using UnityEngine;
using UnityEngine.SceneManagement;
public class ViewManager : MonoBehaviour
{
   public void Playscene(string sceneName) 
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }
    public void Unloadscene(string sceneName)
    {
        SceneManager.UnloadSceneAsync(sceneName);
    }


}
