﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Linq;

public enum QType
{
    Practice,
    Exam
}

public class QuestionPanelScript : MonoBehaviour
{

    private P2DPanel myPanel;
    private List<Question> QuestionList;
    System.Random rnd = new System.Random();

    [SerializeField]
    StartPanelScript startPanel;

    [SerializeField]
    Animator questionAnimator;
    [SerializeField]
    ResultPanelScript ResultsPanel;

    [SerializeField]
    P2DCountDownTimer timerPanel;

    [SerializeField]
    WordsGameManager wordGameManager;

    [SerializeField]
    Button verifyButton, hintButton,skipButton;

    [SerializeField]
    CanvasGroup hintCloud;

    public P2DAmountShower ProgressBarAmount;
    public Text qTitleText, queText;

    private int CurrentQuestionIndex;

    public QuestionButtonScript[] PicQuestionButtons, ChoiceQuestionButtons;

    public AudioClip CorrectSound, WrongSound,NewGroupOpenedClip;

    QType currentQType;
    int currentScore, maxScore;
    static int correctInRow = 0;
    // Use this for initialization
    void Start()
    {
        myPanel = GetComponent<P2DPanel>();

    }

    public void Show(QType type = QType.Practice)
    {
        currentQType = type;
        myPanel.Show();
        qTitleText.text = "";


        hintCloud.DOFade(1, 1).SetDelay(1).OnComplete(() =>
        {
            hintCloud.DOFade(0, 2).SetDelay(3);
        }
        );

        Restart();

    }

    public void Restart()
    {
        verifyButton.interactable = true;
        hintButton.gameObject.SetActive(false);
        List<Question> currentList = new List<Question>();
        if (currentQType == QType.Practice)
        {
            currentList = GetPracticeQuestion();
            timerPanel.gameObject.SetActive(false);
        }
        else
        {
            currentList = GetExamQuestion();

            timerPanel.gameObject.SetActive(true);

        }

        currentScore = 0;

        maxScore = (3 * currentList.Count);
        QuestionList = currentList;
        CurrentQuestionIndex = -1;
        UpdateProgress();
        StartCoroutine(ShowStart());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Setting.MessegeBox.SetMessege("به منو بازمیگردید", "آیا مطمئنید؟", "بازگشت");
            Setting.MessegeBox.OnOkButtonClickEvent += MessegeBox_OnOkButtonClickEvent;
        }
    }

    private void MessegeBox_OnOkButtonClickEvent()
    {
        Hide();
        ResultsPanel.Hide(); 
    }

    private List<Question> GetExamQuestion()
    {
        List<Question> currentList = new List<Question>();

        for (int i = 0; i < GameMng.selectedExam.examCategorys; i++)
        {
            Category currentCategory = GameMng.Instance.GetCategory((QuestionType)i);

            for (int j = 0; j < 3; j++)
            {
                Lesson selectedLesson = currentCategory.lessons[j];
                currentList.AddRange(selectedLesson.questions.OrderBy(x => rnd.Next()).Take(j * 5).ToList());
            }
        }


        return currentList.OrderBy(x => rnd.Next()).Take(GameMng.selectedExam.examQuestions).ToList();

    }

    private List<Question> GetPracticeQuestion()
    {
        List<Question> currentList = new List<Question>();
        Category currentCategory = GameMng.Instance.GetCategory(GameMng.selectedCategory);
        Lesson selectedLesson = currentCategory.lessons[GameMng.selectedLessonIndex - 1];

        while (currentList.Count < 10)
        {
            int nextIndex = rnd.Next(selectedLesson.questions.Count);
            if (!hasQuestionIndex(currentList, nextIndex))
            {
                Question temp = selectedLesson.questions[nextIndex];
                temp.QuestionNum = nextIndex;
                currentList.Add(temp);
            }
           
        }

        return currentList;
    }

    private bool hasQuestionIndex(List<Question> list,int index)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].QuestionNum == index)
                return true;
        }

        return false;
    }

    int examTime = 100;
    private IEnumerator ShowStart()
    {
        string s1, s2;

        if (currentQType == QType.Practice)
        {
            s1 = GameMng.selectedCategory.ToString();
            s2 = "lesson " + GameMng.GetLessonNumberString();
        }
        else
        {
            s2 = GameMng.selectedExam.examTitle;
            s1 = "Exam";
        }

        yield return new WaitForSeconds(1);

        startPanel.Show(s1, s2);

        yield return new WaitForSeconds(2);

        if (currentQType == QType.Exam)
        {
            examTime = (int)(QuestionList.Count * 15f);
            timerPanel.SetTimer(0, 0, 0, examTime);
            timerPanel.OnTimerDoneEvent += TimerPanel_OnTimerDoneEvent;
        }

        ShowNextQuestion();

    }

    private void TimerPanel_OnTimerDoneEvent()
    {
        Setting.notificationMessage.Show("وقت تمامممم".faConvert());
        EndQuestions();
    }

    public void Go2NextLesson()
    {
        if (currentQType == QType.Practice)
        {
            if (GameMng.selectedLessonIndex < 3)
            {
                GameMng.selectedLessonIndex++;
                Restart();
            }
            else if ((int)GameMng.selectedCategory < GameMng.lastCategoryIndex)
            {
                GameMng.selectedCategory = GameMng.selectedCategory + 1;
                GameMng.selectedLessonIndex = 1;
                Restart();

            }
            else
                myPanel.Hide();
        }
        else
        {
            myPanel.Hide();
        }
    }

    public void ShowNextQuestion()
    {

        if (CurrentQuestionIndex < QuestionList.Count - 1)
        {
            verifyButton.interactable = true;
            skipButton.interactable = true;
            CurrentQuestionIndex++;
            SelectQuestionPanel();
            //if(QuestionList[CurrentQuestionIndex].structure != QuestionStruct.WordGame)
            selectedAnswerIndex = 0;
        }
        else
        {
            EndQuestions();
        }

    }

    private void EndQuestions()
    {
        verifyButton.interactable = false;
        skipButton.interactable = false;
        hintButton.gameObject.SetActive(false);
        timerPanel.OnTimerDoneEvent -= TimerPanel_OnTimerDoneEvent;
        ResultType result = GetResultType();
        SaveScore();

        string curentScoreString = currentScore + "/" + maxScore.ToString();
        string bestScoreString = GameMng.GetLessonBestScore(GameMng.selectedCategory, GameMng.selectedLessonIndex) + "/" + maxScore.ToString();

        if(currentQType == QType.Practice)
            GameMng.Instance.SendLessonScore(GameMng.selectedCategory.ToString(), GameMng.selectedLessonIndex, currentScore,QuestionList);
        else
            GameMng.Instance.SendLessonScore("Exam", GameMng.selectedExam.examDegree, currentScore,null);


        if (result >= ResultType.NotBad && currentQType == QType.Practice)
        {
            CheckOpenNextLesson();
            CheckOpenNextCategory();
            GameMng.Instance.CheckAchivmentLessonDone();
        }
        else if(result >= ResultType.NotBad && currentQType == QType.Exam)
        {
            GameMng.Instance.CheckAchivment(timerPanel.GetElepsedTime());
        }

        bool canRestart = true;
        if (currentQType == QType.Exam)
            canRestart = false;

        ResultsPanel.Show(curentScoreString, bestScoreString, result,canRestart);
    }


    private void SaveScore()
    {
        if (currentQType == QType.Practice)
        {
            int lastScore = GameMng.GetLessonBestScore(GameMng.selectedCategory, GameMng.selectedLessonIndex);
            if (currentScore > lastScore)
            {
                GameMng.SetLessonBestScore(GameMng.selectedCategory, GameMng.selectedLessonIndex, currentScore);
            }
        }
        else
        {
            int lastScore = GameMng.GetExamBestScore(GameMng.selectedExam.examTitle);
            if (currentScore > lastScore)
            {
                GameMng.SetExamBestScore(GameMng.selectedExam.examTitle, currentScore);
            }
        }
    }

    private void CheckOpenNextLesson()
    {
        int currentLessonIndex = GameMng.selectedLessonIndex;
        if (currentLessonIndex < 3)
        {
            GameMng.SetLastOpenLesson(GameMng.selectedCategory, currentLessonIndex + 1);
        }
    }

    private void CheckOpenNextCategory()
    {
        int currentCategoryIndex = (int)GameMng.selectedCategory;
        if (currentCategoryIndex < GameMng.lastCategoryIndex)
        {
            int nextCategoryIndex = currentCategoryIndex + 1;

            if (GameMng.GetLastOpenLesson((QuestionType)nextCategoryIndex) == 0)
            {
                StartCoroutine(ShowOpenedCategoryMessage((QuestionType)nextCategoryIndex));
                GameMng.SetLastOpenLesson((QuestionType)nextCategoryIndex, 1);
                //TODO:Sound
            }
        }
    }

    IEnumerator ShowOpenedCategoryMessage(QuestionType nextCat)
    {
        yield return new WaitForSeconds(1.5f);

        //TODO:SOUND, Particle
        Setting.AudioPlayer.PlayOneShot(NewGroupOpenedClip);
        Setting.MessegeBox.SetMessege("یک دسته سوال جدید باز کردی", (nextCat).ToString(), "هوووووراااا");

    }

    private ResultType GetResultType()
    {
        float scorePercent = currentScore / (float)maxScore;

        if (scorePercent < 0.15f)
        {
            return ResultType.Awful;
        }
        else if (scorePercent < 0.3f)
        {
            return ResultType.Bad;
        }
        else if (scorePercent < 0.5f)
        {
            return ResultType.NotGood;
        }
        else if (scorePercent < 0.60f)
        {
            return ResultType.NotBad;
        }
        else if (scorePercent < 0.70f)
        {
            return ResultType.Normal;
        }
        else if (scorePercent < 0.80f)
        {
            return ResultType.Good;
        }
        else if (scorePercent < 0.9f)
        {
            return ResultType.VeryGood;
        }
        else
        {
            return ResultType.Excellent;
        }

    }

    public void SelectQuestionPanel()
    {

        if (QuestionList[CurrentQuestionIndex].structure == QuestionStruct.Choice)
        {
            hintButton.gameObject.SetActive(true);
            ShowChoiceQuestion();
        }
        else if (QuestionList[CurrentQuestionIndex].structure == QuestionStruct.Pic)
        {
            hintButton.gameObject.SetActive(true);
            ShowPicQuestion();
        }
        else if (QuestionList[CurrentQuestionIndex].structure == QuestionStruct.WordGame)
        {
            hintButton.gameObject.SetActive(false);
            //queText.text = "";
            qTitleText.text = "";
            wordGameManager.ShowPanel(QuestionList[CurrentQuestionIndex]);
        }
    }

    public void HintClick()
    {
        if (QuestionList[CurrentQuestionIndex].structure != QuestionStruct.WordGame)
        {
            if (GameMng.GetDiamondNumber() < 5)
                Setting.notificationMessage.Show("الماس کافی نداری".faConvert());
            else
            {
                hintButton.gameObject.SetActive(false);
                GameMng.Instance.RemoveDiamond(5);
                Setting.notificationMessage.Show("5 تا الماس استفاده کردی ".faConvert());
                if (QuestionList[CurrentQuestionIndex].correctAnswer == 2 || QuestionList[CurrentQuestionIndex].correctAnswer == 3)
                {
                    if (QuestionList[CurrentQuestionIndex].structure == QuestionStruct.Choice)
                    {
                        ChoiceQuestionButtons[0].gameObject.SetActive(false);
                        ChoiceQuestionButtons[3].gameObject.SetActive(false);
                    }
                    else
                    {
                        PicQuestionButtons[0].gameObject.SetActive(false);
                        PicQuestionButtons[3].gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (QuestionList[CurrentQuestionIndex].structure == QuestionStruct.Choice)
                    {
                        ChoiceQuestionButtons[1].gameObject.SetActive(false);
                        ChoiceQuestionButtons[2].gameObject.SetActive(false);
                    }
                    else
                    {
                        PicQuestionButtons[1].gameObject.SetActive(false);
                        PicQuestionButtons[2].gameObject.SetActive(false);
                    }
                }
            }
        }

    }

    public void ShowChoiceQuestion()
    {

        // Questions' Title
        qTitleText.text = QuestionList[CurrentQuestionIndex].Title;
        SetQuestion();

        ChoiceQuestionButtons[0].SetButton(QuestionList[CurrentQuestionIndex].options[0]);
        ChoiceQuestionButtons[1].SetButton(QuestionList[CurrentQuestionIndex].options[1]);
        ChoiceQuestionButtons[2].SetButton(QuestionList[CurrentQuestionIndex].options[2]);
        ChoiceQuestionButtons[3].SetButton(QuestionList[CurrentQuestionIndex].options[3]);

        questionAnimator.SetTrigger("ChoiceIn");

    }

    private void SetQuestion()
    {
        if (IsPersianQuestion(QuestionList[CurrentQuestionIndex].Title))
        {
            //queText.lineSpacing = -1;
            //queText.text = QuestionList[CurrentQuestionIndex].Que.faConvert();
            qTitleText.lineSpacing = -1;
            qTitleText.text = QuestionList[CurrentQuestionIndex].Title.faConvert();

        }
        else
        {
            //queText.lineSpacing = 1;
            //queText.text = QuestionList[CurrentQuestionIndex].Que;
            qTitleText.lineSpacing = 1;
            qTitleText.text = QuestionList[CurrentQuestionIndex].Title;
        }
    }

    public void ShowPicQuestion()
    {

        // Questions' Title and Que
        qTitleText.text = QuestionList[CurrentQuestionIndex].Title;
        SetQuestion();

        if (QuestionList[CurrentQuestionIndex].options.Count != 0)
        {
            PicQuestionButtons[0].SetButton(QuestionList[CurrentQuestionIndex].options[0], QuestionList[CurrentQuestionIndex].pics[0]);
            PicQuestionButtons[1].SetButton(QuestionList[CurrentQuestionIndex].options[1], QuestionList[CurrentQuestionIndex].pics[1]);
            PicQuestionButtons[2].SetButton(QuestionList[CurrentQuestionIndex].options[2], QuestionList[CurrentQuestionIndex].pics[2]);
            PicQuestionButtons[3].SetButton(QuestionList[CurrentQuestionIndex].options[3], QuestionList[CurrentQuestionIndex].pics[3]);

        }
        else
        {
            PicQuestionButtons[0].SetButton("", QuestionList[CurrentQuestionIndex].pics[0]);
            PicQuestionButtons[1].SetButton("", QuestionList[CurrentQuestionIndex].pics[1]);
            PicQuestionButtons[2].SetButton("", QuestionList[CurrentQuestionIndex].pics[2]);
            PicQuestionButtons[3].SetButton("", QuestionList[CurrentQuestionIndex].pics[3]);

        }

        questionAnimator.SetTrigger("PicIn");

    }

    private bool IsPersianQuestion(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (Fa.isFarsi(text[i]))
                return true;
        }

        return false;
    }


    int selectedAnswerIndex = 0;
    public void SelectedButtonChange(int index)
    {
        if(index != selectedAnswerIndex)
        {
            selectedAnswerIndex = index;

            string temp = QuestionList[CurrentQuestionIndex].options[index - 1];
            if (temp != string.Empty && !Fa.isFarsi(temp[0]))
            {
                Setting.Speak(temp);
            }
        }
    }

    public void CheckAnswerClick()
    {
        if (selectedAnswerIndex != 0)
        {
            verifyButton.interactable = false;
            skipButton.interactable = false;
            StartCoroutine(VerifyAnswer(selectedAnswerIndex));
        }
        else
            Setting.notificationMessage.Show("گزینه ای انتخاب نشده".faConvert());
    }

    public IEnumerator VerifyAnswer(int index, bool winWordgame = false,int waitSecound = 1)
    {
        QuestionList[CurrentQuestionIndex].selectedAnswer = index;
        //GetComponent<Button>().interactable = false;
        if (index == QuestionList[CurrentQuestionIndex].correctAnswer || winWordgame)
        {
            QuestionList[CurrentQuestionIndex].answerType = 1;
            AfterCorrectAnswer(index);
        }
        else
        {
            QuestionList[CurrentQuestionIndex].answerType = -1;
            AfterWrongAnswer(index);
        }
        UpdateProgress();

        yield return new WaitForSeconds(waitSecound);

        StartCoroutine(HideCurrentShowNextQuestion());
    }

    private void AfterWrongAnswer(int index)
    {
        currentScore -= 1;
        correctInRow = 0;
        Setting.AudioPlayer.PlayOneShot(WrongSound, 0.5f);

        if (QuestionList[CurrentQuestionIndex].structure == QuestionStruct.Pic)
        {
            PicQuestionButtons[QuestionList[CurrentQuestionIndex].correctAnswer - 1].SetGreenCover();
            PicQuestionButtons[index - 1].SetRedCover();
        }
        else if (QuestionList[CurrentQuestionIndex].structure == QuestionStruct.Choice)
        {
            ChoiceQuestionButtons[QuestionList[CurrentQuestionIndex].correctAnswer - 1].SetGreenCover();
            ChoiceQuestionButtons[index - 1].SetRedCover();
        }
    }

    private void AfterCorrectAnswer(int index)
    {
        currentScore += 3;
        correctInRow++;
        StartCoroutine(CheckMaxCorrectInRow());
        Setting.AudioPlayer.PlayOneShot(CorrectSound, 0.5f);

        if (QuestionList[CurrentQuestionIndex].structure == QuestionStruct.Pic)
        {
            PicQuestionButtons[index - 1].SetGreenCover();
        }
        else if (QuestionList[CurrentQuestionIndex].structure == QuestionStruct.Choice)
        {
            ChoiceQuestionButtons[index - 1].SetGreenCover();
        }
    }

    private IEnumerator CheckMaxCorrectInRow()
    {
        if (correctInRow > GameMng.GetMaxCorrectInRow())
        {
            GameMng.SetMaxCorrectInRow(correctInRow);
        }

        yield return 0;
    }

    public void SkipButtonClick()
    {
        skipButton.interactable = false;
        StartCoroutine(ReActiveSkipButton());
        QuestionList[CurrentQuestionIndex].answerType = 0;

        StartCoroutine(HideCurrentShowNextQuestion());
    }

    IEnumerator HideCurrentShowNextQuestion()
    {
        HideCurrentQuestion();

        yield return new WaitForSeconds(1);

        ShowNextQuestion();
    }

    IEnumerator ReActiveSkipButton()
    {
        yield return new WaitForSeconds(2);
        if (CurrentQuestionIndex < QuestionList.Count - 1)
        {
            skipButton.interactable = true;
        }
    }

    private void HideCurrentQuestion()
    {
        if (QuestionList[CurrentQuestionIndex].structure == QuestionStruct.Choice)
        {
            questionAnimator.SetTrigger("ChoiceOut");
        }
        else if (QuestionList[CurrentQuestionIndex].structure == QuestionStruct.Pic)
        {
            questionAnimator.SetTrigger("PicOut");
        }
        else
        {
            WordsGameManager.Instance.Hide();
        }
    }

    public void UpdateProgress()
    {
        ProgressBarAmount.SetMaxAmount(maxScore);
        ProgressBarAmount.SetAmount(currentScore, 1);
    }

    public void Hide()
    {
        HideCurrentQuestion();
        myPanel.Hide();
    }

    public void SpeakRequest()
    {
        Setting.Speak(QuestionList[CurrentQuestionIndex].Que);
    }
}
