﻿using KOTLIN.Interactions;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using KOTLIN.Subtitles;
using Pixelplacement;
using KOTLIN.Translation;
using UnityEngine.Events;
using System.Collections.Generic;
using KOTLIN.Items;
public class GameControllerScript : Singleton<GameControllerScript>
{
	private List<EntranceScript> entrances = new List<EntranceScript>(); //
    KeyCode[] numericKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9 };

    [Obsolete]
	private void Start()
	{
		this.cullingMask = this.camera.cullingMask; // Changes cullingMask in the Camera
		this.audioDevice = base.GetComponent<AudioSource>(); //Get the Audio Source
		this.mode = PlayerPrefs.GetString("CurrentMode"); //Get the current mode
		if (this.mode == "endless") //If it is endless mode
		{
			this.baldiScrpt.endless = true; //Set Baldi use his slightly changed endless anger system
		}
		this.schoolMusic.Play(); //Play the school music
		this.LockMouse(); //Prevent the mouse from moving
		this.UpdateNotebookCount(); //Update the notebook count
		this.itemSelected = 0; //Set selection to item slot 0(the first item slot)
		this.gameOverDelay = 0.5f;

		foreach (EntranceScript entrance in FindObjectsOfTypeAll(typeof(EntranceScript))) //typeall for 2019 support (ew)
		{
			entrances.Add(entrance);
		}

        int[] array = new int[itemSlot.Length];
        for (int i = 0; i < itemSlot.Length; i++)
        {
            array[i] = (int)itemSlot[i].rectTransform.anchoredPosition.x;
        }

		Array.Resize(ref item, itemSlot.Length);
		Array.Resize(ref numericKeys, array.Length);
        this.itemSelectOffset = array;
    }

	private void Update()
	{
		if (!this.learningActive)
		{
			if (Input.GetButtonDown("Pause"))
			{
				if (!this.gamePaused)
				{
					this.PauseGame();
				}
				else
				{
					this.UnpauseGame();
				}
			}
			if (Input.GetKeyDown(KeyCode.Y) & this.gamePaused)
			{
				this.ExitGame();
			}
			else if (Input.GetKeyDown(KeyCode.N) & this.gamePaused)
			{
				this.UnpauseGame();
			}
			if (!this.gamePaused & Time.timeScale != 1f)
			{
				Time.timeScale = 1f;
			}

			if (Time.timeScale != 0f)
			{
                for (int i = 0; i < numericKeys.Length; ++i)
                {
                    if (Input.GetKeyDown(numericKeys[i]))
                    {
                        this.itemSelected = i;
                        this.UpdateItemSelection();
						break;
                    }
                }

                if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                {
                    this.DecreaseItemSelection();
                }
                else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                {
                    this.IncreaseItemSelection();
                }

                if (Input.GetMouseButtonDown(1))
                {
                    this.UseItem();
                }

                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))  //remember to make an input manager
                {
                    Ray ray = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
                    RaycastHit raycastHit;
                    if (Physics.Raycast(ray, out raycastHit, Mathf.Infinity)) //infinity because interatc distance
                    {
                        Interactable interaciton = raycastHit.collider.gameObject.GetComponent<Interactable>();
						if (interaciton != null && interaciton.isActiveAndEnabled && Vector3.Distance(playerTransform.position, interaciton.transform.position) < interaciton.InteractDistance)
							interaciton.Interact();
                    }
                }

			}
		}
		else
		{
			if (Time.timeScale != 0f)
			{
				Time.timeScale = 0f;
			}
		}
		if (this.player.stamina < 0f & !this.warning.activeSelf)
		{
			this.warning.SetActive(true); //Set the warning text to be visible
		}
		else if (this.player.stamina > 0f & this.warning.activeSelf)
		{
			this.warning.SetActive(false); //Set the warning text to be invisible
		}
		if (this.player.gameOver)
		{
			if (this.mode == "endless" && this.notebooks > PlayerPrefs.GetInt("HighBooks") && !this.highScoreText.activeSelf)
			{
				this.highScoreText.SetActive(true);
			}
			Time.timeScale = 0f;
			this.gameOverDelay -= Time.unscaledDeltaTime * 0.5f;
			this.camera.farClipPlane = this.gameOverDelay * 400f; //Set camera farClip 
			this.audioDevice.PlayOneShot(this.aud_buzz);
			if (PlayerPrefs.GetInt("Rumble") == 1)
			{

			}
			if (this.gameOverDelay <= 0f)
			{
				if (this.mode == "endless")
				{
					if (this.notebooks > PlayerPrefs.GetInt("HighBooks"))
					{
						PlayerPrefs.SetInt("HighBooks", this.notebooks);
					}
					PlayerPrefs.SetInt("CurrentBooks", this.notebooks);
				}
				Time.timeScale = 1f;
				SceneManager.LoadScene("GameOver");
			}
		}
		if (this.finaleMode && !this.audioDevice.isPlaying && this.exitsReached == 3)
		{
			this.audioDevice.clip = this.aud_MachineLoop;
			this.audioDevice.loop = true;
			this.audioDevice.Play();
		}
	}

	private void UpdateNotebookCount()
	{
		if (this.mode == "story")
		{
			this.notebookCount.text = this.notebooks.ToString() + $"/{MaxNotebooks} {TranslationManager.Instance.GetTranslationString("Notebooks")}";
		}
		else
		{
			this.notebookCount.text = this.notebooks.ToString() + TranslationManager.Instance.GetTranslationString("Notebooks");
		}
		if (this.notebooks == MaxNotebooks & this.mode == "story")
		{
			this.ActivateFinaleMode();
		}
	}

	public void CollectNotebook()
	{
		this.notebooks++;
		this.UpdateNotebookCount();
	}

	public void LockMouse()
	{
		if (!this.learningActive)
		{
			this.cursorController.LockCursor(); //Prevent the cursor from moving
			this.mouseLocked = true;
			this.reticle.SetActive(true);
		}
	}

	public void UnlockMouse()
	{
		this.cursorController.UnlockCursor(); //Allow the cursor to move
		this.mouseLocked = false;
		this.reticle.SetActive(false);
	}

	public void PauseGame()
	{
		if (!this.learningActive)
		{
			{
				this.UnlockMouse();
			}
			Time.timeScale = 0f;
			this.gamePaused = true;
			this.pauseMenu.SetActive(true);
		}
	}

	public void ExitGame()
	{
		SceneManager.LoadScene("MainMenu");
	}

	public void UnpauseGame()
	{
		Time.timeScale = 1f;
		this.gamePaused = false;
		this.pauseMenu.SetActive(false);
		this.LockMouse();
	}

	public void ActivateSpoopMode()
	{
		this.spoopMode = true; //Tells the game its time for spooky
        foreach (EntranceScript entrance in entrances)
        {
			entrance.Lower(this); 
        }
        this.baldiTutor.SetActive(false); //Turns off Baldi(The one that you see at the start of the game)
		this.baldi.SetActive(true); //Turns on Baldi
        this.principal.SetActive(true); //Turns on Principal
        this.crafters.SetActive(true); //Turns on Crafters
        this.playtime.SetActive(true); //Turns on Playtime
        this.gottaSweep.SetActive(true); //Turns on Gotta Sweep
        this.bully.SetActive(true); //Turns on Bully
        this.firstPrize.SetActive(true); //Turns on First-Prize
		//this.TestEnemy.SetActive(true); //Turns on Test-Enemy
		this.audioDevice.PlayOneShot(this.aud_Hang); //Plays the hang sound
		this.learnMusic.Stop(); //Stop all the music
		this.schoolMusic.Stop();
	}

	private void ActivateFinaleMode()
	{
		this.finaleMode = true;
        foreach (EntranceScript entrance in entrances)
        {
            entrance.Raise();
        }
    }

	public void GetAngry(float value) //Make Baldi get angry
	{
		if (!this.spoopMode)
		{
			this.ActivateSpoopMode();
		}
		this.baldiScrpt.GetAngry(value);
	}

	public void ActivateLearningGame()
	{
		//this.camera.cullingMask = 0; //Sets the cullingMask to nothing
		this.learningActive = true;
		this.UnlockMouse(); //Unlock the mouse
		this.tutorBaldi.Stop(); //Make tutor Baldi stop talking
		if (!this.spoopMode) //If the player hasn't gotten a question wrong
		{
			this.schoolMusic.Stop(); //Start playing the learn music
			this.learnMusic.Play();
		}
	}

	public void DeactivateLearningGame(GameObject subject)
	{
		this.camera.cullingMask = this.cullingMask; //Sets the cullingMask to Everything
		this.learningActive = false;
		UnityEngine.Object.Destroy(subject);
		this.LockMouse(); //Prevent the mouse from moving
		if (this.player.stamina < 100f) //Reset Stamina
		{
			this.player.stamina = 100f;
		}
		if (!this.spoopMode) //If it isn't spoop mode, play the school music
		{
			this.schoolMusic.Play();
			this.learnMusic.Stop();
		}
		if (this.notebooks == 1 & !this.spoopMode) // If this is the players first notebook and they didn't get any questions wrong, reward them with a quarter
		{
			this.quarter.SetActive(true);
			this.tutorBaldi.PlayOneShot(this.aud_Prize);
		}
		else if (this.notebooks == MaxNotebooks & this.mode == "story") // Plays the all 7 notebook sound
		{
			this.audioDevice.PlayOneShot(this.aud_AllNotebooks, 0.8f);
		}
	}

	private void IncreaseItemSelection()
	{
		this.itemSelected++;
		if (this.itemSelected > itemSlot.Length -1)
		{
			this.itemSelected = 0;
		}
		this.itemSelect.anchoredPosition = new Vector3((float)this.itemSelectOffset[this.itemSelected], itemSelect.anchoredPosition.y, 0f); //Moves the item selector background(the red rectangle)
		this.UpdateItemName();
	}

	private void DecreaseItemSelection()
	{
		this.itemSelected--;
		if (this.itemSelected < 0)
		{
			this.itemSelected = itemSlot.Length - 1;
		}
		this.itemSelect.anchoredPosition = new Vector3((float)this.itemSelectOffset[this.itemSelected], itemSelect.anchoredPosition.y, 0f); //Moves the item selector background(the red rectangle)
		this.UpdateItemName();
	}

	private void UpdateItemSelection()
	{
		this.itemSelect.anchoredPosition = new Vector3((float)this.itemSelectOffset[this.itemSelected], itemSelect.anchoredPosition.y, 0f); //Moves the item selector background(the red rectangle)
		this.UpdateItemName();
	}

	//basic v0.3 prerelease
	public void CollectItem(int item_ID)
	{
        int emptySlotIndex = -1;
        for (int i = 0; i < this.item.Length; i++)
        {
            if (this.item[i] == 0)
            {
                emptySlotIndex = i;
                break;
            }
        }

        int slotIndex = emptySlotIndex != -1 ? emptySlotIndex : this.itemSelected;

        this.item[slotIndex] = item_ID;
        this.itemSlot[slotIndex].texture = itemManager.items[item_ID].ItemSprite;

        itemManager.items[this.item[itemSelected]].OnPickup?.Invoke();
        this.UpdateItemName();
    }

	private void UseItem()
	{
		if (this.item[this.itemSelected] != 0)
		{
			itemManager.items[this.item[itemSelected]].OnUse?.Invoke(); 
		}
	}

	public IEnumerator BootAnimation()
	{
		float time = 15f;
		float height = 375f;
		Vector3 position = default;
		this.boots.gameObject.SetActive(true);
		while (height > -375f)
		{
			height -= 375f * Time.deltaTime;
			time -= Time.deltaTime;
			position = this.boots.localPosition;
			position.y = height;
			this.boots.localPosition = position;
			yield return null;
		}
		position = this.boots.localPosition;
		position.y = -375f;
		this.boots.localPosition = position;
		this.boots.gameObject.SetActive(false);
		while (time > 0f)
		{
			time -= Time.deltaTime;
			yield return null;
		}
		this.boots.gameObject.SetActive(true);
		while (height < 375f)
		{
			height += 375f * Time.deltaTime;
			position = this.boots.localPosition;
			position.y = height;
			this.boots.localPosition = position;
			yield return null;
		}
		position = this.boots.localPosition;
		position.y = 375f;
		this.boots.localPosition = position;
		this.boots.gameObject.SetActive(false);
		yield break;
	}

	public void ResetItem()
	{
		this.item[this.itemSelected] = 0;
		this.itemSlot[this.itemSelected].texture = itemManager.items[0].ItemSprite;
		this.UpdateItemName();
	}

	public void LoseItem(int id)
	{
        this.item[id] = 0;
        this.itemSlot[id].texture = itemManager.items[0].ItemSprite;
        this.UpdateItemName();
    }

	private void UpdateItemName()
	{
		this.itemText.text = TranslationManager.Instance.GetTranslationString(itemManager.items[this.item[this.itemSelected]].NameKey);
	}

	public void ExitReached()
	{
		this.exitsReached++;
		if (this.exitsReached == 1)
		{
			RenderSettings.ambientLight = Color.red; //Make everything red and start player the weird sound
			//RenderSettings.fog = true;
			this.audioDevice.PlayOneShot(this.aud_Switch, 0.8f);
			this.audioDevice.clip = this.aud_MachineQuiet;
			this.audioDevice.loop = true;
			this.audioDevice.Play();
		}
		if (this.exitsReached == 2) //Play a sound
		{
			this.audioDevice.volume = 0.8f;
			this.audioDevice.clip = this.aud_MachineStart;
			this.audioDevice.loop = true;
			this.audioDevice.Play();
		}
		if (this.exitsReached == 3) //Play a even louder sound
		{
			this.audioDevice.clip = this.aud_MachineRev;
			this.audioDevice.loop = false;
			this.audioDevice.Play();
		}
	}

	public void DespawnCrafters()
	{
		this.crafters.SetActive(false); //Make Arts And Crafters Inactive
	}

	public void Fliparoo()
	{
		this.player.height = 6f;
		this.player.fliparoo = 180f;
		this.player.flipaturn = -1f;
		Camera.main.GetComponent<CameraScript>().offset = new Vector3(0f, -1f, 0f);
	}

	[SerializeField] private ItemManager itemManager;
	public int MaxNotebooks;

	[Space()]
	public CursorControllerScript cursorController;

	public PlayerScript player;

	public Transform playerTransform;

	public Transform cameraTransform;

	public new Camera camera;

	private int cullingMask;

	public GameObject baldiTutor;

	public GameObject baldi;

	public BaldiScript baldiScrpt;

	public AudioClip aud_Prize;

	public AudioClip aud_PrizeMobile;

	public AudioClip aud_AllNotebooks;

	public GameObject principal;

	public GameObject crafters;

	public GameObject playtime;

	public PlaytimeScript playtimeScript;

	public GameObject gottaSweep;

	public GameObject bully;

	public GameObject firstPrize;

	public GameObject TestEnemy;

	public FirstPrizeScript firstPrizeScript;

	public GameObject quarter;

	public AudioSource tutorBaldi;

	public RectTransform boots;

	public string mode;

	public int notebooks;

	public GameObject[] notebookPickups;

	public int failedNotebooks;

	public bool spoopMode;

	public bool finaleMode;

	public bool debugMode;

	public bool mouseLocked;

	public int exitsReached;

	public int itemSelected;

	public int[] item = new int[0];

	public RawImage[] itemSlot = new RawImage[3];

	public TMP_Text itemText;	

	public GameObject bsodaSpray;

	public GameObject alarmClock;

	public TMP_Text notebookCount;

	public GameObject pauseMenu;

	public GameObject highScoreText;

	public GameObject warning;

	public GameObject reticle;

	public RectTransform itemSelect;

	private int[] itemSelectOffset;

	private bool gamePaused;

	private bool learningActive;

	private float gameOverDelay;

	[HideInInspector] public AudioSource audioDevice;

	public AudioClip aud_Soda;

	public AudioClip aud_Spray;

	public AudioClip aud_buzz;

	public AudioClip aud_Hang;

	public AudioClip aud_MachineQuiet;

	public AudioClip aud_MachineStart;

	public AudioClip aud_MachineRev;

	public AudioClip aud_MachineLoop;

	public AudioClip aud_Switch;

	public AudioSource schoolMusic;

	public AudioSource learnMusic;

	//private Player playerInput;
}
