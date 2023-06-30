using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Ink.Runtime;
using UnityEngine.EventSystems;

public class DialogueManager : MonoBehaviour
{
    [Header("Params")]
    public float typingSpeed = 0.04f;

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public GameObject continueIcon;
    public TextMeshProUGUI dialogueText;

    public TextMeshProUGUI displayNameText;

    public GameObject npcImage;
    public GameObject playerImage;

    [Header("Choices UI")]
    public GameObject[] choices;
    private TextMeshProUGUI[] choicesText;
    public GameObject choicesBox;

    private Story currentStory;

    public bool dialogueIsPlaying { get; private set; }
    public bool canContinueToNextLine = false;
    public bool canSkip = false;
    public bool submitSkip;

    private static DialogueManager instance;
    private const string SPEAKER_TAG = "speaker";
    private const string PORTRAIT_TAG = "portrait";

    public Animator arrowAnim;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Found more than one Dialogue Manager in the scene");
        }
        instance = this;
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);

        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach(GameObject choice in choices)
        {
            choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }
    }

    private void Update()
    {
        // return right away if dialogue isn't playing
        if (!dialogueIsPlaying)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            submitSkip = true;
        }

        // Disable all other keys except the spacebar
        if (canContinueToNextLine && currentStory.currentChoices.Count == 0)
        {
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (keyCode != KeyCode.Space && Input.GetKeyDown(keyCode))
                {
                    // Ignore key press
                    continue;
                }
            }

            // Continue the story when the spacebar is pressed
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ContinueStory();
            }
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON)
    {
        currentStory = new Story(inkJSON.text);

        dialogueIsPlaying = true;
        dialoguePanel.SetActive(true);

        ContinueStory();
    }

    private IEnumerator Typewriter(string line)
    {
        // empty the dialogue text
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        HideChoices();
        arrowAnim.enabled = false;
        continueIcon.SetActive(false);

        submitSkip = false;

        canContinueToNextLine = false;

        StartCoroutine(CanSkip());

        foreach(char ch in line.ToCharArray())
        {
            if(canSkip && submitSkip)
            {
                submitSkip = false;
                dialogueText.maxVisibleCharacters = line.Length;
                break;
            }

            dialogueText.maxVisibleCharacters++;
            yield return new WaitForSeconds(typingSpeed);
        }

        continueIcon.SetActive(true);

        arrowAnim.enabled = true;

        DisplayChoices();
        canContinueToNextLine = true;
        canSkip = false;
    }

    private void HideChoices()
    {
        foreach(GameObject choiseButton in choices)
        {
            choiseButton.SetActive(false);
        }
    }

    private IEnumerator CanSkip()
    {
        canSkip = false; //Making sure the variable is false.
        yield return null;
        canSkip = true;
    }

    public void ExitDialogueMode()
    {
        dialogueIsPlaying = false;
        dialogueText.text = "";
        dialoguePanel.SetActive(false);
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            StartCoroutine(Typewriter(currentStory.Continue()));
            
            HandleTags(currentStory.currentTags);
        }
        else
        {
            ExitDialogueMode();
        }
    }

    private void HandleTags(List<string> currentTags)
    {
        foreach(string tag in currentTags)
        {
            string[] splitTag = tag.Split(':');
            if(splitTag.Length != 2)
            {
                Debug.LogError("Tag could not be parsed: " + tag);
            }
            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            switch (tagKey)
            {
                case SPEAKER_TAG:
                    displayNameText.text = tagValue;
                    break;
                case PORTRAIT_TAG:
                    Debug.Log(tag);
                    if (tagValue == "NPC")
                    {
                        playerImage.SetActive(false);
                        npcImage.SetActive(true);
                    }
                    else if(tagValue == "PLAYER")
                    {
                        npcImage.SetActive(false);
                        playerImage.SetActive(true);
                    }
                    break;
                default:
                    Debug.LogWarning("Tag came in but is not currently being handled: " + tagValue);
                    break;
            }
        }
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        if(currentChoices.Count > 0)
        {
            choicesBox.SetActive(true);
        }

        // defensive check to make sure our UI can support the number of choices coming in
        if (currentChoices.Count > choices.Length)
        {
            Debug.LogError("More choices were given than the UI can support. Number of choices given: "
                + currentChoices.Count);
        }

        int index = 0;
        // enable and initialize the choices up to the amount of choices for this line of dialogue
        foreach (Choice choice in currentChoices)
        {
            choices[index].SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }
        // go through the remaining choices the UI supports and make sure they're hidden
        for (int i = index; i < choices.Length; i++)
        {
            choices[i].SetActive(false);
        }

        StartCoroutine(SelectFirstChoice());
    }

    private IEnumerator SelectFirstChoice()
    {
        // Event System requires we clear it first, then wait
        // for at least one frame before we set the current selected object.
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(choices[0]);
    }

    public void MakeChoice(int choiceIndex)
    {
        if (canContinueToNextLine)
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
            {
                // Ignore keypad Enter and Return key press
                return;
            }

            currentStory.ChooseChoiceIndex(choiceIndex);
            choicesBox.SetActive(false);
            Debug.Log(choiceIndex);
        }
    }

}
