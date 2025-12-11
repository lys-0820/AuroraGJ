using UnityEngine;
using UnityEngine.UI;
public class UIManager : MonoBehaviour
{
    [Header("UI 元素")]
    public GameObject pauseMenu;    // 暂停菜单面板
    public GameObject helpMenu;     // 帮助菜单面板
    public GameObject startMenu;    // 开始菜单面板

    public AudioSource backgroundMusic; // 背景音乐 AudioSource
    public AudioSource ambientSound;     // 环境音效 AudioSource
    // private bool isPaused = false;
    private bool isStarted = false;
    public GameObject PauseButton;
    void Start()
    {
        pauseMenu.SetActive(false);
        helpMenu.SetActive(false);
        startMenu.SetActive(true);
        ambientSound.Stop();
        backgroundMusic.Play();
        PauseButton.SetActive(false);
    }
    void Update()
    {
        
        if(Input.anyKeyDown)
        {
            if(!isStarted)
        {
            Debug.Log("Game Started");
            startGame();
            isStarted = true;
        }
        
        }
        // 按下 Esc 键切换暂停状态
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void startGame()
    {
        startMenu.SetActive(false);
        pauseMenu.SetActive(false);
        helpMenu.SetActive(false);
        backgroundMusic.Stop();
        ambientSound.Play();
        PauseButton.SetActive(true);
    }
    public void TogglePause()
    {
        pauseMenu.SetActive(true);
        helpMenu.SetActive(false);
        Time.timeScale = 0f;      // 所有基于时间的更新都会停下（物理、动画、粒子等）
        AudioListener.pause = true; // 暂停所有声音
    }
    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        helpMenu.SetActive(false);
        Time.timeScale = 1f;      
        AudioListener.pause = false;
    }

    public void ShowHelpInfo()
    {
        pauseMenu.SetActive(false);
        helpMenu.SetActive(true);
    }
    public void HideHelpInfo()
    {
        helpMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }
    public void quitGame()
    {
        Application.Quit();
    }
}