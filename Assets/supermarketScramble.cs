using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class supermarketScramble : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;
    [SerializeField] private AudioSource AudioSrc;

    [SerializeField] private AudioClip SupermarketMusic;
    [SerializeField] private List<GameObject> ItemTexts;
    [SerializeField] private GameObject ListObject;
    [SerializeField] private GameObject AislesParent;
    [SerializeField] private KMSelectable ListToggle;
    [SerializeField] private AudioClip ToggleClip;
    [SerializeField] private List<GameObject> ItemButtons;
    [SerializeField] private List<KMSelectable> AisleArrows;
    [SerializeField] private List<GameObject> Aisles;
    [SerializeField] private TextMesh HighlightText;
    [SerializeField] private TextMesh AisleNumberText;
    [SerializeField] private List<KMSelectable> SlotButtons;
    [SerializeField] private AudioClip SuccessClip;
    [SerializeField] private GameObject CheckoutAisle;
    [SerializeField] private GameObject SolveButton;
    [SerializeField] private TextMesh SolveText;

    List<string> items = new List<string>() { "CUCUMBER", "CELERY", "TOMATO", "SPINACH", "BROCCOLI", "LETTUCE", "ZUCCHINI", "ASPARAGUS", "GARLIC", "CARROT", "ONION", "POTATOES", "CHICKEN", "SAUSAGES", "PORKCHOP", "STEAK", "TURKEY", "HOTDOGS", "BACON", "SALMON", "SHRIMP", "SUSHI", "MUFFINS", "CUPCAKES", "DONUTS", "COOKIES", "DANISHES", "BAGUETTE", "TORTILLAS", "BAGELS", "BREAD", "CROISSANTS", "BANANA", "PINEAPPLE", "COCONUT", "AVOCADO", "LEMON", "ORANGE", "CHERRIES", "GRAPES", "SOUP", "TUNA", "CEREAL", "CRACKERS", "PEANUTS", "COCOA", "MOLASSES", "CINNAMON", "OATMEAL", "SUGAR", "COFFEE", "FLOUR", "BATTERIES", "RAZORS", "DEODORANT", "SHAMPOO", "BLEACH", "SPONGES", "DETERGENT", "NAPKINS", "DIAPERS", "TISSUES", "POPCORN", "CHOCOLATE", "PIZZA", "ICE CREAM", "POPSICLES", "MILK", "JUICE", "CHEESE", "YOGURT", "EGGS", "MARGARINE", "BUTTER", "RICE", "HONEY", "PASTA", "JELLY", "MUSTARD", "VINEGAR" };

    List<string> modUnscrItems = new List<string>() { };
    List<string> modScrItems = new List<string>() { };

    bool moduleStarted;
    bool listView;
    string selectedItem;
    int curAisle;
    string[] slottedItems = new string[8];
    int correctCount;
    bool success;
    int hours, minutes, seconds = 0;

    List<string> TPDirList = new List<string>() { "left", "right", "l", "r" };

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        GetComponent<KMSelectable>().OnFocus += delegate () { ModuleSelected(); };
        GetComponent<KMSelectable>().OnDefocus += delegate () { AudioSrc.Pause(); };
        ListToggle.OnInteract += delegate () { ToggleList(); return false; };
        foreach (GameObject button in ItemButtons) {
            button.GetComponent<KMSelectable>().OnInteract += delegate () { ItemPress(button); return false; };
            button.GetComponent<KMSelectable>().OnSelect += delegate () { HighlightText.text = button.GetComponent<MarketItem>().ItemName == "ICECREAM" ? "ICE CREAM" : button.GetComponent<MarketItem>().ItemName; };
            button.GetComponent<KMSelectable>().OnHighlightEnded += delegate () { HighlightText.text = ""; };
        }
        foreach (KMSelectable slot in SlotButtons) {
            slot.OnInteract += delegate () { SlotPress(SlotButtons.IndexOf(slot)); return false; };
            slot.OnSelect += delegate () { HighlightText.text = slottedItems[SlotButtons.IndexOf(slot)]; };
            slot.OnHighlightEnded += delegate () { HighlightText.text = ""; };
        }
        foreach (KMSelectable arrow in AisleArrows) {
            arrow.OnInteract += delegate () { CycleAisles(AisleArrows.IndexOf(arrow)); return false; };
        }

        SolveButton.GetComponent<KMSelectable>().OnInteract += delegate () { Solved(); return false; };
    }

    void ModuleSelected()
    {
        if (!moduleStarted)
        {
            moduleStarted = true;
            StartCoroutine("Timer");
            AudioSrc.clip = SupermarketMusic;
            AudioSrc.Play();

            listView = true;
            ListObject.SetActive(true);
            foreach (GameObject iText in ItemTexts)
            {
                iText.SetActive(true);
            }
            AislesParent.SetActive(false);
            for (int i = 0; i < Aisles.Count; i++)
            {
                Aisles[i].SetActive(i == 0);
            }

            GenItems();
            ScrambleItems();
        }
        AudioSrc.UnPause();
    }

    void ToggleList()
    {
        ListToggle.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ListToggle.transform);
        Audio.PlaySoundAtTransform(ToggleClip.name, transform);
        listView = !listView;
        ListObject.SetActive(listView);
        AislesParent.SetActive(!listView);
    }

    void ItemPress(GameObject itemObj)
    {
        itemObj.GetComponent<KMSelectable>().AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, itemObj.transform);
        selectedItem = itemObj.GetComponent<MarketItem>().ItemName;
    }

    void SlotPress(int slotIdx)
    {
        SlotButtons[slotIdx].GetComponent<KMSelectable>().AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SlotButtons[slotIdx].transform);
        if (selectedItem != "" & !success)
        {
            bool itemWasCorrect = slottedItems[slotIdx] == modUnscrItems[slotIdx];

            slottedItems[slotIdx] = selectedItem == "ICECREAM" ? "ICE CREAM" : selectedItem;
            HighlightText.text = selectedItem;

            bool itemCorrect = slottedItems[slotIdx] == modUnscrItems[slotIdx];
            if (!itemWasCorrect)
            {
                correctCount += itemCorrect ? 1 : 0;
            }
            else if (!itemCorrect)
            {
                correctCount -= 1;
            }
            Log($"Put {(selectedItem == "ICECREAM" ? "ICE CREAM" : TitleString(selectedItem))} in slot {slotIdx + 1}. That is {(itemCorrect ? "correct" : "incorrect")}. {correctCount}/8 correct items.");
            success = correctCount == 8;
            if (success)
            {
                Log("All items collected. Checkout is permanently open!");
                Audio.PlaySoundAtTransform(SuccessClip.name, transform);
                SolveButton.SetActive(true);
            }
        }
    }

    void CycleAisles(int dir)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, AisleArrows[dir].transform);
        curAisle += dir * 2 - 1;
        if (curAisle == Aisles.Count)
        {
            curAisle = Aisles.Count - 1;
        }
        else if (curAisle == -2)
        {
            curAisle = -1;
        }

        AisleNumberText.text = curAisle == -1 ? "C" : (curAisle + 1).ToString();
        AisleNumberText.fontSize = curAisle > 8 ? 105 : 200;
        for (int i = 0; i < Aisles.Count; i++)
        {
            Aisles[i].SetActive(i == curAisle);
        }
        CheckoutAisle.SetActive(curAisle == -1);
    }

    void Solved()
    {
        SolveButton.GetComponent<KMSelectable>().AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, SolveButton.transform);
        GetComponent<KMBombModule>().HandlePass();
        ModuleSolved = true;

        ListObject.SetActive(false);
        AislesParent.SetActive(false);
        ListToggle.gameObject.SetActive(false);
        AudioSrc.Stop();
        DisplayTimer();
    }

    void DisplayTimer()
    {
        SolveText.gameObject.SetActive(true);
        string timerText = "";
        if (hours > 0)
        {
            timerText += hours + ":";
        }
        timerText += $"{(minutes < 10 ? "0" + minutes.ToString() : minutes.ToString())}:{(seconds < 10 ? "0" + seconds.ToString() : seconds.ToString())}";
        SolveText.text = $"YOU'VE FINISHED 1ST!\nYOUR TIME:\n{timerText}";
    }

    void GenItems()
    {
        List<string> tempItems = items.ConvertAll(x => x);
        string logUnscrambled = "";
        for (int i = 0; i < 8; i++)
        {
            modUnscrItems.Add(tempItems[Rnd.Range(0, tempItems.Count - 1)]);
            tempItems.Remove(modUnscrItems[i]);
            logUnscrambled += TitleString(modUnscrItems[i]);
            logUnscrambled += (i == 8 ? "." : ", ");
        }
        Log($"Unscrambled Items are: {logUnscrambled}");
    }

    void ScrambleItems()
    {
        string logScrambled = "";
        for (int i = 0; i < 8; i++)
        {
            modScrItems.Add(ScrambleString(modUnscrItems[i]));
            ItemTexts[i].GetComponent<TextMesh>().text = $"{i + 1}. {modScrItems[i]}";
            logScrambled += modScrItems[i];
            logScrambled += (i == 8 ? "." : ", ");
        }
        Log($"Items after Scrambling: {logScrambled}");
    }

    void Start()
    {
        ListObject.SetActive(true);
        AislesParent.SetActive(false);
        SolveButton.SetActive(false);
        foreach (GameObject iText in ItemTexts)
        {
            iText.SetActive(false);
        }
    }

    string TitleString(string n)
    {
        return n[0].ToString().ToUpper() + n.Substring(1, n.Length - 1).ToLower();
    }

    string ScrambleString(string n)
    {
        string s = "";
        n = n.Replace(" ", "");
        while (n.Length > 0)
        {
            int idx = Rnd.Range(0, n.Length - 1);
            s += n[idx];
            n = n.Substring(0, idx) + n.Substring(idx + 1);
        }
        return s;
    }

    void Log(string arg)
    {
        Debug.Log($"[Supermarket Scramble #{ModuleId}] {arg}");
    }

    IEnumerator Timer()
    {
        while (!ModuleSolved)
        {
            yield return new WaitForSeconds(1);
            seconds += 1;
            if (seconds == 60)
            {
                minutes += 1;
                seconds = 0;
                if (minutes == 60)
                {
                    hours += 1;
                    minutes = 0;
                }
            }
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} select> to select the module (to start it). Use <!{0} list> to toggle the list. Use <!{0} left/l/right/r #> to go left/right # times or 1 time if # not specified. Use <!{0} inspect #> to highlight the #th button in the aisle. Use <!{0} put #1 #2> to put the #1th item from the aisle into the #2th cart slot. Use <!{0} slot #> to highlight the #th slot to see what's slotted in it. Use <!{0} solvebutton> to press the Solve Button in the checkout lane.";
#pragma warning restore 4141

    IEnumerator ProcessTwitchCommand(string Command)
    {
        var commandArgs = Command.ToLowerInvariant().Split(new[] { ' ' }, 3);
        if (commandArgs.Length < 1)
        {
            yield return "sendtochaterror No command supplied!";
        }
        switch (commandArgs[0])
        {
            case "select":
                if (moduleStarted)
                {
                    yield return "sendtochaterror Module already started!";
                }
                else
                {
                    GetComponent<KMSelectable>().OnFocus();
                    yield return null;
                    ModuleSelected();
                    yield return new WaitForSeconds(0.1f);
                }
                GetComponent<KMSelectable>().OnDefocus();
                break;
            case "list":
                if (!moduleStarted)
                {
                    yield return "sendtochaterror Module not started!";
                }
                else
                {
                    GetComponent<KMSelectable>().OnFocus();
                    yield return null;
                    ListToggle.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                GetComponent<KMSelectable>().OnDefocus();
                break;
            case "left":
            case "right":
            case "l":
            case "r":
                if (!moduleStarted)
                {
                    yield return "sendtochaterror Module not started!";
                }
                else if (listView)
                {
                    yield return "sendtochaterror You are currently viewing the list!";
                }
                else
                {
                    int TPTimes = 1;
                    if (commandArgs.Length == 2)
                    {
                        bool tryParse = Int32.TryParse(commandArgs[1], out TPTimes);
                        if (!tryParse)
                        {
                            yield return "sendtochaterror Invalid amount of times!";
                        }
                    }
                    else if (commandArgs.Length > 2)
                    {
                        yield return "sendtochaterror Command too long!";
                    }

                    int TPDir = TPDirList.IndexOf(commandArgs[0]) % 2;
                    GetComponent<KMSelectable>().OnFocus();
                    yield return null;
                    for (int i = 0; i < TPTimes; i++)
                    {
                        AisleArrows[TPDir].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                GetComponent<KMSelectable>().OnDefocus();
                break;
            case "inspect":
                if (listView)
                {
                    yield return "sendtochaterror You are currently viewing the list!";
                }
                else if (curAisle == -1)
                {
                    yield return "sendtochaterror You are at the checkout lane!";
                }
                else
                {
                    if (commandArgs.Length < 2)
                    {
                        yield return "sendtochaterror Command too short!";
                    }
                    else if (commandArgs.Length > 2)
                    {
                        yield return "sendtochaterror Command too long!";
                    }
                    else
                    {
                        int TPButtonIdx;
                        bool tryParse = Int32.TryParse(commandArgs[1], out TPButtonIdx);
                        if (!tryParse)
                        {
                            yield return "sendtochaterror Invalid button number!";
                        }
                        else if (TPButtonIdx < 1 || TPButtonIdx > Aisles[curAisle].transform.childCount)
                        {
                            yield return "sendtochaterror Button number not in range!";
                        }
                        else
                        {
                            GetComponent<KMSelectable>().OnFocus();
                            yield return null;
                            Aisles[curAisle].transform.GetChild(TPButtonIdx - 1).GetComponent<KMSelectable>().OnSelect();
                            yield return new WaitForSeconds(1f);
                            Aisles[curAisle].transform.GetChild(TPButtonIdx - 1).GetComponent<KMSelectable>().OnHighlightEnded();
                            yield return new WaitForSeconds(0.5f);
                        }
                    }
                }
                GetComponent<KMSelectable>().OnDefocus();
                break;
            case "put":
                if (listView)
                {
                    yield return "sendtochaterror You are currently viewing the list!";
                }
                else if (curAisle == -1)
                {
                    yield return "sendtochaterror You are at the checkout lane!";
                }
                else
                {
                    if (commandArgs.Length < 3)
                    {
                        yield return "sendtochaterror Command too short!";
                    }
                    else if (commandArgs.Length > 3)
                    {
                        yield return "sendtochaterror Command too long!";
                    }
                    else
                    {
                        int TPItemIdx;
                        bool tryParseItem = Int32.TryParse(commandArgs[1], out TPItemIdx);
                        if (!tryParseItem)
                        {
                            yield return "sendtochaterror Invalid item number!";
                        }
                        else if (TPItemIdx < 1 || TPItemIdx > Aisles[curAisle].transform.childCount)
                        {
                            yield return "sendtochaterror Button number not in range!";
                        }
                        else
                        {
                            int TPSlotIdx;
                            bool tryParseSlot = Int32.TryParse(commandArgs[2], out TPSlotIdx);
                            if (!tryParseSlot)
                            {
                                yield return "sendtochaterror Invalid slot number!";
                            }
                            else if (TPSlotIdx < 1 || TPSlotIdx > 8)
                            {
                                yield return "sendtochaterror Slot number not in range!";
                            }
                            else
                            {
                                GetComponent<KMSelectable>().OnFocus();
                                yield return null;
                                Aisles[curAisle].transform.GetChild(TPItemIdx - 1).GetComponent<KMSelectable>().OnInteract();
                                yield return new WaitForSeconds(0.1f);
                                SlotButtons[TPSlotIdx - 1].OnInteract();
                                SlotButtons[TPSlotIdx - 1].OnHighlightEnded();
                            }
                        }
                    }
                }
                GetComponent<KMSelectable>().OnDefocus();
                break;
            case "slot":
                if (listView)
                {
                    yield return "sendtochaterror You are currently viewing the list!";
                }
                else
                {
                    if (commandArgs.Length < 2)
                    {
                        yield return "sendtochaterror Command too short!";
                    }
                    else if (commandArgs.Length > 2)
                    {
                        yield return "sendtochaterror Command too long!";
                    }
                    else
                    {
                        int TPSlotIdx;
                        bool tryParseSlot = Int32.TryParse(commandArgs[1], out TPSlotIdx);
                        if (tryParseSlot)
                        {
                            if (TPSlotIdx < 1 || TPSlotIdx > 8)
                            {
                                yield return "sendtochaterror Slot number not in range!";
                            }
                            else
                            {
                                GetComponent<KMSelectable>().OnFocus();
                                yield return null;
                                SlotButtons[TPSlotIdx - 1].GetComponent<KMSelectable>().OnSelect();
                                yield return new WaitForSeconds(1f);
                                SlotButtons[TPSlotIdx - 1].GetComponent<KMSelectable>().OnHighlightEnded();
                            }
                        }
                        else
                        {
                            yield return "sendtochaterror Invalid slot number!";
                        }
                    }
                }
                GetComponent<KMSelectable>().OnDefocus();
                break;
            case "solvebutton":
                if (listView)
                {
                    yield return "sendtochaterror You are currently viewing the list!";
                }
                else if (curAisle != -1)
                {
                    yield return "sendtochaterror You are not at the checkout lane!";
                }
                else if (!success)
                {
                    yield return "sendtochaterror Solve button unavailable!";
                }
                else
                {
                    GetComponent<KMSelectable>().OnFocus();
                    yield return null;
                    SolveButton.GetComponent<KMSelectable>().OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                GetComponent<KMSelectable>().OnDefocus();
                break;
            default:
                yield return "sendtochaterror Invalid command!";
                break;
        }
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (!moduleStarted) { yield return null; GetComponent<KMSelectable>().OnFocus(); };
        if (listView) { yield return null; ListToggle.OnInteract(); }
        int TPAisle = curAisle;
        for (int i = 0; i < (14 - TPAisle); i++)
        {
            yield return null;
            AisleArrows[1].OnInteract();
            yield return new WaitForSeconds(0.01f);
        }

        for (int i = 0; i < 14; i++)
        {
            for (int j = 0; j < Aisles[curAisle].transform.childCount; j++)
            {
                string checkItem = Aisles[curAisle].transform.GetChild(j).GetComponent<MarketItem>().ItemName;
                if (modUnscrItems.Contains(checkItem))
                {
                    Aisles[curAisle].transform.GetChild(j).GetComponent<KMSelectable>().OnInteract();
                    yield return new WaitForSeconds(0.01f);
                    SlotButtons[modUnscrItems.IndexOf(checkItem)].OnInteract();
                    yield return new WaitForSeconds(0.01f);
                } 
            }
            AisleArrows[0].OnInteract();
            yield return new WaitForSeconds(0.01f);
        }

        SolveButton.GetComponent<KMSelectable>().OnInteract();
    }
}
