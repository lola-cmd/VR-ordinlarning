
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewBehaviourScript : MonoBehaviour
{

    public void StartLearn(){
        SceneManager.LoadSceneAsync(1);
    }

    public void StartTest(){
        SceneManager.LoadSceneAsync(2);
    }
   
}
