using BlazorQuizApp.Models;
using System.Text.Json;

namespace BlazorQuizApp.Services
{
    public class QuizService
    {
        private readonly string _dataFilePath;
        private int _nextId = 1;

        // State
        public List<QuizQuestion> Questions { get; private set; } = new List<QuizQuestion>();
        public int CurrentQuestionIndex { get; private set; }
        public int SelectedOptionId { get; private set; }
        public bool IsAnswered { get; private set; }
        public bool IsQuizCompleted { get; private set; }
        public bool IsCurrentAnswerCorrect { get; private set; }
        public QuizResult Result { get; private set; } = new QuizResult();

        public QuizService(IWebHostEnvironment environment)
        {
            _dataFilePath = Path.Combine(environment.ContentRootPath, "Data", "questions.json");
            var dataDirectory = Path.GetDirectoryName(_dataFilePath);
            if (dataDirectory != null)
            {
                Directory.CreateDirectory(dataDirectory);
            }
            LoadQuestions();
        }

        public void LoadQuestions()
        {
            if (!File.Exists(_dataFilePath))
            {
                Questions = GetDefaultQuestions();
            }
            else
            {
                try
                {
                    var json = File.ReadAllText(_dataFilePath);
                    Questions = JsonSerializer.Deserialize<List<QuizQuestion>>(json) ?? GetDefaultQuestions();
                }
                catch
                {
                    Questions = GetDefaultQuestions();
                }
            }

            if (Questions.Count > 0)
            {
                _nextId = Questions.Max(q => q.Id) + 1;
            }

            RestartQuiz();
        }

        public QuizQuestion GetCurrentQuestion()
        {
            return Questions[CurrentQuestionIndex];
        }

        public QuizQuestion? GetQuestionById(int id)
        {
            return Questions.FirstOrDefault(q => q.Id == id);
        }

        public void SelectOption(QuizOption option)
        {
            if (IsAnswered) return;

            SelectedOptionId = option.Id;
            IsAnswered = true;
            var currentQuestion = GetCurrentQuestion();
            IsCurrentAnswerCorrect = currentQuestion.CorrectOptionId == option.Id;

            if (IsCurrentAnswerCorrect)
            {
                Result.CorrectAnswers++;
            }
        }

        public void NextQuestion()
        {
            if (CurrentQuestionIndex < Questions.Count - 1)
            {
                CurrentQuestionIndex++;
                IsAnswered = false;
                SelectedOptionId = -1;
            }
            else
            {
                IsQuizCompleted = true;
                Result.TotalQuestions = Questions.Count;
            }
        }

        public void RestartQuiz()
        {
            CurrentQuestionIndex = 0;
            SelectedOptionId = -1;
            IsAnswered = false;
            IsQuizCompleted = false;
            Result = new QuizResult();
        }

        public void AddQuestion(NewQuestionForm questionForm)
        {
            var question = new QuizQuestion
            {
                Id = _nextId++,
                QuestionText = questionForm.QuestionText,
                Options = new List<QuizOption>(),
                CorrectOptionId = questionForm.CorrectOptionIndex + 1
            };

            for (int i = 0; i < questionForm.OptionTexts.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(questionForm.OptionTexts[i]))
                {
                    question.Options.Add(new QuizOption { Id = i + 1, Text = questionForm.OptionTexts[i] });
                }
            }

            if (question.Options.Count >= 2 && !string.IsNullOrWhiteSpace(question.QuestionText))
            {
                Questions.Add(question);
                SaveQuestions();
            }
        }

        public void DeleteQuestion(int id)
        {
            var questionToRemove = Questions.FirstOrDefault(q => q.Id == id);
            if (questionToRemove != null)
            {
                Questions.Remove(questionToRemove);
                SaveQuestions();
            }
        }

        public void UpdateQuestion(int id, NewQuestionForm questionForm)
        {
            var questionToUpdate = GetQuestionById(id);
            if (questionToUpdate != null)
            {
                questionToUpdate.QuestionText = questionForm.QuestionText;
                questionToUpdate.CorrectOptionId = questionForm.CorrectOptionIndex + 1;
                questionToUpdate.Options.Clear();

                for (int i = 0; i < questionForm.OptionTexts.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(questionForm.OptionTexts[i]))
                    {
                        questionToUpdate.Options.Add(new QuizOption { Id = i + 1, Text = questionForm.OptionTexts[i] });
                    }
                }

                SaveQuestions();
            }
        }

        private void SaveQuestions()
        {
            var json = JsonSerializer.Serialize(Questions, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFilePath, json);
        }

        private List<QuizQuestion> GetDefaultQuestions()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    Id = 1,
                    QuestionText = "Which of the following statements is true regarding dependency injection?",
                    Options = new List<QuizOption>
                    {
                        new QuizOption { Id = 1, Text = "Dependency injection is a design pattern that allows you to loosely couple your app's dependencies." },
                        new QuizOption { Id = 2, Text = "ASP.NET Core comes with a built-in service container that supports dependency injection." },
                        new QuizOption { Id = 3, Text = "ASP.NET Core's service container supports three service lifetimes: transient, scoped, and singleton." },
                        new QuizOption { Id = 4, Text = "All of the above." }
                    },
                    CorrectOptionId = 4
                },
                new QuizQuestion
                {
                    Id = 2,
                    QuestionText = "What is the behavior of a service registered with the singleton service lifetime?",
                    Options = new List<QuizOption>
                    {
                        new QuizOption { Id = 1, Text = "A new instance of the service is created for each request." },
                        new QuizOption { Id = 2, Text = "A single instance of the service is created for the lifetime of the app." },
                        new QuizOption { Id = 3, Text = "A new instance of the service is created each time an instance is requested." },
                        new QuizOption { Id = 4, Text = "None of the above." }
                    },
                    CorrectOptionId = 2
                },
                new QuizQuestion
                {
                    Id = 3,
                    QuestionText = "Which service lifetime is most appropriate for a stateless service?",
                    Options = new List<QuizOption>
                    {
                        new QuizOption { Id = 1, Text = "Transient" },
                        new QuizOption { Id = 2, Text = "Scoped" },
                        new QuizOption { Id = 3, Text = "Singleton" },
                        new QuizOption { Id = 4, Text = "It depends on the specific requirements" }
                    },
                    CorrectOptionId = 1
                }
            };
        }
    }
}
