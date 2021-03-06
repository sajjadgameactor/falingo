﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
//using Backtory.Core.Public;
using GameSparks.Api.Requests;
using GameSparks.Api.Responses;
using GameSparks.Core;
using System.IO;
using UnityEngine.Networking;

public enum QuestionMode { Easy, Intermed, Diffy }
public enum QuestionType { Animals, Actions,Grammer1, Colors, Food, Fruits, BodyParts,Grammer2, Weather, Toys, Sports, Clothes, Jobs,Grammer3, Transport, SchoolThings, Objects,Grammer4 }
public enum QuestionStruct { Choice, Pic, WordGame }

public delegate void OnScoreChange(QuestionType type);

public class GameMng : SingletonMahsa<GameMng>
{
    public const int lastCategoryIndex = 17;
    public static QuestionType selectedCategory;
    public static int selectedLessonIndex;
    public static ExamButtonScript selectedExam;

    public static event OnScoreChange onScoreChangeEvent;

    [SerializeField]
    List<GameObject> mainPanels;

    [SerializeField]
    List<Category> categorys;

    [SerializeField]
    List<ExamButtonScript> exams;

    [SerializeField]
    AchivmentPanelScript achivmentBigPanel;

    [SerializeField]
    Transform achivmentParent;
    List<AchivmentScript> achivmentList = new List<AchivmentScript>(32);

    [SerializeField]
    P2DAmountShower xpShower,diamondShower;

    [SerializeField]
    ExamPanelScript examPanel;

    [SerializeField]
    Sprite diamondSprite;

    [SerializeField]
    AudioClip collectClip,removeClip;

    private float delay = 1;

    public LessonPanelScript LessonPanel;
    public QuestionPanelScript questionPanel;

    private void Awake()
    {

#if UNITY_EDITOR
        //PlayerPrefs.DeleteAll(); //danger*****
#endif

        if (GetLastOpenLesson(0) == 0)
        {
            SetLastOpenLesson(0, 1);
        }
    }

   

    void Start()
    {
        Setting.initSetting();
        FillAchivmentList();
        SetMainPanel(0);

        onScoreChangeEvent += GameMng_onScoreChangeEvent;

        UpdateXpShower();
        diamondShower.SetAmount(GetDiamondNumber());

        //AddDiamond(1000); //***** remove

    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(!questionPanel.gameObject.activeInHierarchy)
            {
                if(LessonPanel.gameObject.activeInHierarchy)
                {
                    LessonPanel.Hide();
                }
                else
                {
                    Setting.MessegeBox.SetMessege("آیا از برنامه خارج میشوید؟");
                    Setting.MessegeBox.OnOkButtonClickEvent += MessegeBox_OnOkButtonClickEvent;
                }
            }
        }
    }

    private void MessegeBox_OnOkButtonClickEvent()
    {
        Application.Quit();
    }

    public void ShowAchivmentBigPanel(string message, Sprite icon, string shareName)
    {
        achivmentBigPanel.Show(message, icon, shareName);
    }

    private void FillAchivmentList()
    {
        for (int i = 0; i < achivmentParent.childCount; i++)
        {
            AchivmentScript temp = achivmentParent.GetChild(i).GetComponent<AchivmentScript>();
            if (temp != null)
                achivmentList.Add(temp);
        }
    }

    private void GameMng_onScoreChangeEvent(QuestionType type)
    {
        UpdateXpShower();
    }

    private void UpdateXpShower()
    {
        int currentScore = 0;
        int maxScore = (lastCategoryIndex + 1) * 100;
        int excelentLessons = 0;
        int excelentCategory = 0;

        int currentExcelentLesson;
        int currentCategoryScore;
        for (int i = 0; i <= lastCategoryIndex; i++)
        {
            currentCategoryScore = GetCategoryScore((QuestionType)i, out currentExcelentLesson);
            currentScore += currentCategoryScore;
            excelentLessons += currentExcelentLesson;
            if (currentCategoryScore == 100)
                excelentCategory++;

            CheckAchivment(excelentCategory, excelentLessons);
        }

        foreach (var item in exams)
        {
            maxScore += (item.examQuestions);
            currentScore += GetExamBestScore(item.examTitle);
        }

        xpShower.SetMaxAmount(maxScore);
        xpShower.SetAmount(currentScore);

        SendScore(currentScore);

    }

    private void CheckAchivment(int excelentCategory, int excelentLessons)
    {
        foreach (var item in achivmentList)
        {
            if(item.achivmentType == AchivmentType.ExcelentCategory)
            {
                if(!item.IsCollected() && item.requestedAmount <= excelentCategory)
                {
                    item.Open2Collect();
                }
            }
            else if (item.achivmentType == AchivmentType.ExcelentLesson)
            {
                if (!item.IsCollected() && !item.IsReady2Collect() && item.requestedAmount <= excelentLessons)
                {
                    item.Open2Collect();
                }
            }
        }
    }

    public void CheckAchivment(TimeSpan examSucessTime)
    {
        foreach (var item in achivmentList)
        {
            if (item.achivmentType == AchivmentType.ExamTime)
            {
                if (!item.IsCollected() && !item.IsReady2Collect() && item.requestedAmount >= examSucessTime.TotalMinutes)
                {
                    item.Open2Collect();
                }
            }

        }
    }

    public void CheckAchivmentLessonDone()
    {
        int openedLesson = 0;
        for (int i = 0; i <= lastCategoryIndex; i++)
        {
            openedLesson += GetLastOpenLesson((QuestionType)i);
        }

        Debug.Log("Opened Lesson:" + openedLesson.ToString());
        foreach (var item in achivmentList)
        {
            if (item.achivmentType == AchivmentType.PassLesson)
            {
                if (!item.IsCollected() && !item.IsReady2Collect() && item.requestedAmount <= openedLesson)
                {
                    item.Open2Collect();
                }
            }
        }
    }

    private void CheckAchivmentCorrectInRow()
    {
        int correctInRow = GetMaxCorrectInRow();
        foreach (var item in achivmentList)
        {
            if (item.achivmentType == AchivmentType.CorrectInRow)
            {
                if (!item.IsCollected() && !item.IsReady2Collect() && item.requestedAmount <= correctInRow)
                {
                    item.Open2Collect();
                }
            }

        }
    }

    private void CheckAchivmentExcelentExam()
    {
        int excelentExams = 0;

        foreach (var item in exams)
        {
            if (item.IsDoneExcelent())
                excelentExams++;
        }

        foreach (var item in achivmentList)
        {
            if (item.achivmentType == AchivmentType.ExcelentExam)
            {
                if (!item.IsCollected() && !item.IsReady2Collect() && item.requestedAmount <= excelentExams)
                {
                    item.Open2Collect();
                }
            }

        }
    }

    public void AddDiamond(int amount)
    {
        int total = GetDiamondNumber() + amount;
        SetDiamondNumber(total);
        Setting.collectManager.Collect(diamondSprite, Vector3.zero, new Vector3(-288, 603),amount, collectClip);
        diamondShower.SetAmount(total);
    }

    public void RemoveDiamond(int amount)
    {
        int total = GetDiamondNumber() - amount;
        SetDiamondNumber(total);
        Setting.collectManager.Collect(diamondSprite, new Vector3(-288, 603), Vector3.zero, amount, null);
        diamondShower.SetAmount(total);
    }

    const string diamondKey = "diamondKey";
    public static int GetDiamondNumber()
    {
        int val = 0;
        P2DSecurety.SecureLocalLoad(diamondKey, out val);
        return val;
    }

    public static void SetDiamondNumber( int value)
    {
        P2DSecurety.SecureLocalSave(diamondKey , value);

    }

    const string correctInRowKey = "correctInRowKey";
    public static int GetMaxCorrectInRow()
    {
        int val = 0;
        P2DSecurety.SecureLocalLoad(correctInRowKey, out val);
        return val;
    }

    public static void SetMaxCorrectInRow(int value)
    {
        P2DSecurety.SecureLocalSave(correctInRowKey, value);
        Instance.CheckAchivmentCorrectInRow();
    }


    public void SetMainPanel(int panelIndex)
    {
        foreach (var item in mainPanels)
        {
            item.SetActive(false);
        }

        mainPanels[panelIndex].SetActive(true);

        if(panelIndex == 1)
        {
        }

    }

    public void ShowLessonPanel(QuestionType category)
    {
        selectedCategory = category;
        LessonPanel.Show();
    }

    public void ShowQuestionPanel()
    {
        questionPanel.Show();
    }

    public void ShowExamPanel()
    {
        examPanel.Show();
    }

    public void ShowExam()
    {
        questionPanel.Show(QType.Exam);
    }

    public Category GetCategory(QuestionType categoryType)
    {
        return categorys.First(s => s.questionType == categoryType);
    }


    const string lessonOfLevelKey = "lessonOfLevelKey";

    public static int GetLastOpenLesson(QuestionType category)
    {
        int lesson = 0;
        P2DSecurety.SecureLocalLoad(lessonOfLevelKey + category.ToString(), out lesson);
        return lesson;
    }

    public static void SetLastOpenLesson(QuestionType category, int value)
    {
        P2DSecurety.SecureLocalSave(lessonOfLevelKey + category.ToString(), value);

        if (onScoreChangeEvent != null)
        {
            onScoreChangeEvent(category);
        }
    }


    public static int GetLessonBestScore(QuestionType Category, int lessonNumber)
    {
        int lesson = 0;
        P2DSecurety.SecureLocalLoad(lessonOfLevelKey + Category.ToString() + lessonNumber.ToString(), out lesson);
        return lesson;
    }

    public static void SetLessonBestScore(QuestionType Category, int lessonNumber, int value)
    {
        P2DSecurety.SecureLocalSave(lessonOfLevelKey + Category.ToString() + lessonNumber.ToString(), value);

        if (onScoreChangeEvent != null)
        {
            onScoreChangeEvent(Category);
        }
    }

    public static int GetExamBestScore(string examName)
    {
        int score = 0;
        P2DSecurety.SecureLocalLoad(lessonOfLevelKey + examName, out score);
        return score;
    }

    public static void SetExamBestScore(string examName, int value)
    {
        P2DSecurety.SecureLocalSave(lessonOfLevelKey + examName, value);

        if (onScoreChangeEvent != null)
        {
            onScoreChangeEvent((QuestionType)lastCategoryIndex);
        }

       Instance.CheckAchivmentExcelentExam();
    }

    public static int GetCategoryScore(QuestionType questionType)
    {
        int score = 0;
        for (int i = 1; i <= 3; i++)
        {
            score += GetLessonBestScore(questionType, i);
        }

        if (score == 90)
            score = 100;

        return score;
    }

    public static int GetCategoryScore(QuestionType questionType,out int excelentLessonNumber)
    {
        int score = 0;
        excelentLessonNumber = 0;
        for (int i = 1; i <= 3; i++)
        {
            int lessonScore = GetLessonBestScore(questionType, i);
            if (lessonScore == 30)
                excelentLessonNumber++;

            score += lessonScore;
        }

        if (score == 90)
            score = 100;

        return score;
    }

    public static string GetLessonNumberString()
    {
        switch (selectedLessonIndex)
        {
            case 1:
                return "one";
            case 2:
                return "two";
            case 3:
                return "three";
            default:
                return "one";
        }
    }

    public void SendScore(int score)
    {
        try
        {

            new LogEventRequest().SetEventKey("ScoreChange").SetEventAttribute("Score", score)
                .Send((response) => {
                    if(response.HasErrors)
                    {
                        Debug.LogError(response.Errors.BaseData["DETAILS"].ToString());
                    }
                    GSData scriptData = response.ScriptData;
                });

            //// Step 1: Creating parameters for GameOver event
            //List<BacktoryGameEvent.FieldValue> fieldValues = new List<BacktoryGameEvent.FieldValue>()
            // {
            //     new BacktoryGameEvent.FieldValue("Score", score),
            //};

            //// Step 2: Creating GameOver event and filling its data
            //BacktoryGameEvent backtoryGameEvent = new BacktoryGameEvent()
            //{
            //    Name = "ScoreChange",
            //    FieldsAndValues = fieldValues
            //};

            //// Step 3: Sending event to server
            //backtoryGameEvent.SendInBackground(backtoryResponse =>
            //{
            //    // Checking callback from server
            //    if (backtoryResponse.Successful)
            //    {
            //        Debug.Log("saved event successfully");
            //    }
            //    else
            //    {
            //        Debug.Log(backtoryResponse.Message);
            //        // do something based on BactoryResponse.Code
            //    }
            //});
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

    }


    private string shareText;
    private static string gameLink = "ببین تو لینگولند چی کردم!!!";
    private string subject;
    public static IEnumerator TakeImage()
    {

        // wait for graphics to render
        yield return new WaitForEndOfFrame();
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- PHOTO
        // create the texture
        Texture2D screenTexture = new Texture2D(Screen.width, (int)(Screen.height), TextureFormat.RGB24, false);

        // put buffer into texture
        screenTexture.ReadPixels(new Rect(0f, 0, Screen.width, (Screen.height)), 0, 0);

        // apply
        screenTexture.Apply();
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- PHOTO

        yield return screenTexture;

    }

    public static void ShareImage(Texture2D shareTexture,string shareName)
    {
        try
        {
            GameMng.Instance.SendShareInfo(shareName);
        }
        catch { }
        byte[] dataToSave = shareTexture.EncodeToJPG();

        string destination = Path.Combine(Application.persistentDataPath, System.DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".png");

        File.WriteAllBytes(destination, dataToSave);

        if (!Application.isEditor)
        {
            // block to open the file and share it ------------START
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
            intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", "file://" + destination);
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uriObject);
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), gameLink);
            //intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), gameLink);
            intentObject.Call<AndroidJavaObject>("setType", "image/jpeg");
            AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");

            // option one WITHOUT chooser:
            currentActivity.Call("startActivity", intentObject);

            // option two WITH chooser:
            //AndroidJavaObject jChooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "YO BRO! WANNA SHARE?");
            //currentActivity.Call("startActivity", jChooser);

            // block to open the file and share it ------------END

        }
    }

    public void SendLessonScore(string category,int lesson,int score,List<Question> questionList)
    {
        ScoreHistory temp = new ScoreHistory();
        temp.id = 999;
        temp.category = category;
        temp.lesson = lesson;
        temp.score = score;
        Setting.historyList.Add(temp);
        string username = GetUsername();
        string uri = string.Format("http://sajjadcv.ir/lingoland/addscore.php?user_id={0}&name={1}&category={2}&lesson={3}&score={4}", SystemInfo.deviceUniqueIdentifier, username, category, lesson, score);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        if(questionList != null)
        {
            for (int i = 0; i < questionList.Count; i++)
            {
                var item = questionList[i];
                formData.Add(new MultipartFormDataSection(i.ToString(),string.Format("{0} {1} {2} {3} {4} {5}", item.QuestionNum, item.answerType, item.selectedAnswer, item.correctAnswer, item.Mode, item.structure)));

            }
           
        }
       

        StartCoroutine(Setting.SendData(uri,formData));
    }

    //public void SendTempData()
    //{
    //    string uri = string.Format("http://sajjadcv.ir/lingoland/addscore.php?user_id={0}&name={1}&category={2}&lesson={3}&score={4}", SystemInfo.deviceUniqueIdentifier, "سجی", "test", 1, 10);

    //    List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
      
    //    formData.Add(new MultipartFormDataSection("dat",string.Format("{0} {1} {2} {3}", 2, 1, "easy", "pic")));
    //    formData.Add(new MultipartFormDataSection("dat2",string.Format("{0} {1} {2} {3}", 1, -1, "mid", "choice")));

    //    StartCoroutine(Setting.SendData(uri,formData));

    //}

    private static string GetUsername()
    {
        string username = "none";
        try
        {
            username = Setting.authResponse.DisplayName;
        }
        catch { }

        return username;
    }

    public void SendAchivmentEarn(string achivmentName)
    {

        string uri = string.Format("http://sajjadcv.ir/lingoland/addachivment.php?user_id={0}&name={1}&achivment_name={2}", SystemInfo.deviceUniqueIdentifier, GetUsername(),achivmentName);

        StartCoroutine(Setting.SendData(uri));
    }

    public void SendShareInfo(string shareName)
    {

        string uri = string.Format("http://sajjadcv.ir/lingoland/addshareinfo.php?user_id={0}&name={1}&share_name={2}", SystemInfo.deviceUniqueIdentifier, GetUsername(), shareName);

        StartCoroutine(Setting.SendData(uri));
    }




    //IEnumerator SendData(string url)
    //{
    //    WWWForm parameters = new WWWForm();
    //    parameters.AddField("myField", "myData");
    //    parameters.AddField("Game Name", "Mario Kart");
    //    byte[] rawData = null;
    //    if (parameters != null)
    //    {
    //        rawData = parameters.data;
    //    }
    //    using (var request = new WWW(url, rawData))
    //    {
    //        yield return request;
    //        if (string.IsNullOrEmpty(request.error))
    //        {// ok
    //        }
    //        else
    //        {// error
    //        }
    //        // kill request after work is done.
    //        request.Dispose();
    //    }
    //}
}
