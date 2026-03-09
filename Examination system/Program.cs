using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExaminationSystem
{
    // exam modes
    public enum ExamMode
    {
        Queued,
        Starting,
        Finished
    }

    // ============ Answer ============
    public class Answer : ICloneable, IComparable<Answer>
    {
        public int Id { get; set; }
        public string Text { get; set; }

        public Answer() : this(0, "No Answer") { }

        public Answer(int id) : this(id, "No Answer") { }

        public Answer(int id, string text)
        {
            Id = id;
            Text = text;
        }

        public object Clone()
        {
            return new Answer(Id, Text);
        }

        public int CompareTo(Answer other)
        {
            if (other == null) return 1;
            return Id.CompareTo(other.Id);
        }

        public override string ToString()
        {
            return $"{Id}. {Text}";
        }

        public override bool Equals(object obj)
        {
            if (obj is Answer other)
                return Id == other.Id && Text == other.Text;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Text);
        }
    }

    // ============ AnswerList ============
    public class AnswerList : List<Answer>, ICloneable
    {
        public object Clone()
        {
            AnswerList newList = new AnswerList();
            foreach (var ans in this)
                newList.Add((Answer)ans.Clone());
            return newList;
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, this);
        }
    }

    // ============ Question (abstract base) ============
    public abstract class Question : ICloneable, IComparable<Question>
    {
        public string Header { get; set; }
        public string Body { get; set; }
        public int Marks { get; set; }
        public AnswerList Answers { get; set; }
        public List<int> RightAnswerIds { get; set; }

        public Question() : this("Question", "No Body", 0, new AnswerList()) { }

        public Question(string header, string body, int marks)
            : this(header, body, marks, new AnswerList()) { }

        public Question(string header, string body, int marks, AnswerList answers)
        {
            Header = header;
            Body = body;
            Marks = marks;
            Answers = answers;
            RightAnswerIds = new List<int>();
        }

        // each question type shows itself differently
        public abstract void Show();

        public virtual object Clone()
        {
            Question copy = (Question)this.MemberwiseClone();
            copy.Answers = (AnswerList)Answers.Clone();
            copy.RightAnswerIds = new List<int>(RightAnswerIds);
            return copy;
        }

        public int CompareTo(Question other)
        {
            if (other == null) return 1;
            return Marks.CompareTo(other.Marks);
        }

        public override string ToString()
        {
            return $"{Header}: {Body} ({Marks} marks)";
        }

        public override bool Equals(object obj)
        {
            if (obj is Question other)
                return Header == other.Header && Body == other.Body && Marks == other.Marks;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Header, Body, Marks);
        }
    }

    // ============ Question Types ============
    public class TrueFalseQuestion : Question
    {
        public TrueFalseQuestion() : base()
        {
            Header = "True/False";
            Answers.Add(new Answer(1, "True"));
            Answers.Add(new Answer(2, "False"));
        }

        public TrueFalseQuestion(string body, int marks)
            : base("True/False", body, marks, new AnswerList())
        {
            Answers.Add(new Answer(1, "True"));
            Answers.Add(new Answer(2, "False"));
        }

        public override void Show()
        {
            Console.WriteLine(ToString());
            foreach (var ans in Answers)
                Console.WriteLine("  " + ans);
        }
    }

    public class ChooseOneQuestion : Question
    {
        public ChooseOneQuestion() : base()
        {
            Header = "Choose One";
        }

        public ChooseOneQuestion(string body, int marks, AnswerList answers)
            : base("Choose One", body, marks, answers) { }

        public override void Show()
        {
            Console.WriteLine(ToString());
            foreach (var ans in Answers)
                Console.WriteLine("  " + ans);
        }
    }

    public class ChooseAllQuestion : Question
    {
        public ChooseAllQuestion() : base()
        {
            Header = "Choose All";
        }

        public ChooseAllQuestion(string body, int marks, AnswerList answers)
            : base("Choose All", body, marks, answers) { }

        public override void Show()
        {
            Console.WriteLine(ToString());
            foreach (var ans in Answers)
                Console.WriteLine("  " + ans);
        }
    }

    // ============ QuestionList ============
    // inherits List<Question> and logs every added question to a file
    public class QuestionList : List<Question>
    {
        public string FilePath { get; set; }

        public QuestionList(string filePath)
        {
            FilePath = filePath;
        }

        // can't override Add because List<T>.Add is not virtual
        // so we use 'new' to hide it and add logging
        public new void Add(Question q)
        {
            base.Add(q);

            // log the question to file
            using (StreamWriter sw = new StreamWriter(FilePath, true))
            {
                sw.WriteLine(q.ToString());
                foreach (var ans in q.Answers)
                    sw.WriteLine("   " + ans);
                sw.WriteLine("----------------------------");
            }
        }
    }

    // ============ Subject ============
    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Subject() : this(0, "Unknown") { }

        public Subject(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name} (ID: {Id})";
        }
    }

    // ============ Student ============
    public class Student
    {
        public string Name { get; set; }

        public Student(string name)
        {
            Name = name;
        }

        // this gets called when exam starts
        public void OnExamStarted(Exam exam)
        {
            Console.WriteLine($"[Notification] Hey {Name}! Exam '{exam.Subject.Name}' is starting now.");
        }
    }

    // ============ Exam (abstract base) ============
    public delegate void ExamStartedHandler(Exam exam);

    public abstract class Exam : ICloneable, IComparable<Exam>
    {
        public int TimeInMinutes { get; set; }
        public int NumOfQuestions { get; set; }
        public Subject Subject { get; set; }
        public QuestionList Questions { get; set; }

        // stores student answers per question
        public Dictionary<Question, List<int>> StudentAnswers { get; set; }
        public ExamMode Mode { get; set; }

        public event ExamStartedHandler ExamStarted;

        public Exam() : this(60, new Subject(), new QuestionList("default.txt")) { }

        public Exam(int time, Subject subj, QuestionList questions)
        {
            TimeInMinutes = time;
            Subject = subj;
            Questions = questions;
            NumOfQuestions = questions.Count;
            StudentAnswers = new Dictionary<Question, List<int>>();
            Mode = ExamMode.Queued;
        }

        public void StartExam()
        {
            Mode = ExamMode.Starting;
            ExamStarted?.Invoke(this); // notify all subscribed students
        }

        public void FinishExam()
        {
            Mode = ExamMode.Finished;
        }

        public abstract void ShowExam();

        // take answers from user for each question
        public virtual void TakeAnswers()
        {
            Console.WriteLine("\n--- Answer the questions below ---");

            foreach (var q in Questions)
            {
                q.Show();
                Console.Write("Your answer (enter ID or IDs separated by comma): ");
                string input = Console.ReadLine();

                // parse the answer IDs
                List<int> chosenIds = new List<int>();
                foreach (var part in input.Split(','))
                {
                    if (int.TryParse(part.Trim(), out int id))
                        chosenIds.Add(id);
                }

                StudentAnswers[q] = chosenIds;
                Console.WriteLine();
            }
        }

        // compare student answers with correct answers
        public virtual int CorrectExam()
        {
            int totalGrade = 0;

            foreach (var entry in StudentAnswers)
            {
                Question q = entry.Key;
                List<int> studentAns = entry.Value;

                // check if student got it exactly right
                bool correct = q.RightAnswerIds.Count == studentAns.Count
                               && !q.RightAnswerIds.Except(studentAns).Any();

                if (correct)
                    totalGrade += q.Marks;
            }

            return totalGrade;
        }

        public virtual object Clone()
        {
            Exam copy = (Exam)this.MemberwiseClone();
            copy.Subject = new Subject(Subject.Id, Subject.Name);

            QuestionList copiedQuestions = new QuestionList(Questions.FilePath);
            foreach (var q in Questions)
                copiedQuestions.Add((Question)q.Clone());

            copy.Questions = copiedQuestions;
            copy.StudentAnswers = new Dictionary<Question, List<int>>();
            return copy;
        }

        public int CompareTo(Exam other)
        {
            if (other == null) return 1;
            return TimeInMinutes.CompareTo(other.TimeInMinutes);
        }

        public override string ToString()
        {
            return $"Subject: {Subject.Name} | Time: {TimeInMinutes} min | Questions: {Questions.Count} | Mode: {Mode}";
        }

        public override bool Equals(object obj)
        {
            if (obj is Exam other)
                return TimeInMinutes == other.TimeInMinutes
                    && Subject.Name == other.Subject.Name
                    && Questions.Count == other.Questions.Count;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TimeInMinutes, Subject?.Name, Questions?.Count);
        }
    }

    // ============ Practice Exam ============
    // shows correct answers at the end
    public class PracticeExam : Exam
    {
        public PracticeExam() : base() { }

        public PracticeExam(int time, Subject subj, QuestionList questions)
            : base(time, subj, questions) { }

        public override void ShowExam()
        {
            Console.WriteLine("===== Practice Exam =====");
            Console.WriteLine(this);

            StartExam();
            TakeAnswers();

            int grade = CorrectExam();
            FinishExam();

            Console.WriteLine($"\nYour grade: {grade}");

            // show correct answers (practice exam only)
            Console.WriteLine("\n--- Correct Answers ---");
            foreach (var q in Questions)
            {
                Console.WriteLine(q.Body);
                Console.WriteLine("Answer: " + string.Join(", ", q.RightAnswerIds));
            }
        }
    }

    // ============ Final Exam ============
    // doesn't show correct answers
    public class FinalExam : Exam
    {
        public FinalExam() : base() { }

        public FinalExam(int time, Subject subj, QuestionList questions)
            : base(time, subj, questions) { }

        public override void ShowExam()
        {
            Console.WriteLine("===== Final Exam =====");
            Console.WriteLine(this);

            StartExam();
            TakeAnswers();

            int grade = CorrectExam();
            FinishExam();

            Console.WriteLine($"\nYour grade: {grade}");
            Console.WriteLine("Exam done. Good luck with your results!");
        }
    }

    // ============ Generic Exam Manager ============
    // T must be Exam or a subclass of it
    public class ExamManager<T> where T : Exam
    {
        private List<T> examList = new List<T>();

        public void AddExam(T exam)
        {
            examList.Add(exam);
        }

        public void ShowAll()
        {
            foreach (var exam in examList)
                Console.WriteLine(exam);
        }

        public T GetByIndex(int i)
        {
            return examList[i];
        }
    }

    // ============ Main Program ============
    internal class Program
    {
        static void Main(string[] args)
        {
            Subject csharp = new Subject(1, "C# Programming");

            // students who will be notified
            Student st1 = new Student("Ahmed");
            Student st2 = new Student("Sara");

            QuestionList qList = new QuestionList("questions_log.txt");

            // Q1 - true/false
            TrueFalseQuestion q1 = new TrueFalseQuestion("C# supports OOP.", 2);
            q1.RightAnswerIds.Add(1); // True
            qList.Add(q1);

            // Q2 - choose one
            AnswerList q2Answers = new AnswerList()
            {
                new Answer(1, "int"),
                new Answer(2, "string"),
                new Answer(3, "banana"),
                new Answer(4, "double")
            };
            ChooseOneQuestion q2 = new ChooseOneQuestion("Which is NOT a C# data type?", 3, q2Answers);
            q2.RightAnswerIds.Add(3);
            qList.Add(q2);

            // Q3 - choose all
            AnswerList q3Answers = new AnswerList()
            {
                new Answer(1, "Inheritance"),
                new Answer(2, "Encapsulation"),
                new Answer(3, "Polymorphism"),
                new Answer(4, "Cooking")
            };
            ChooseAllQuestion q3 = new ChooseAllQuestion("Select the OOP concepts:", 5, q3Answers);
            q3.RightAnswerIds.AddRange(new[] { 1, 2, 3 });
            qList.Add(q3);

            // create both exam types
            PracticeExam pExam = new PracticeExam(30, csharp, qList);
            FinalExam fExam = new FinalExam(20, csharp, qList);

            // subscribe students to exam notifications
            pExam.ExamStarted += st1.OnExamStarted;
            pExam.ExamStarted += st2.OnExamStarted;

            fExam.ExamStarted += st1.OnExamStarted;
            fExam.ExamStarted += st2.OnExamStarted;

            // let user pick exam type
            Console.WriteLine("Which exam do you want to take?");
            Console.WriteLine("1. Practice Exam");
            Console.WriteLine("2. Final Exam");
            Console.Write("Choice: ");

            string pick = Console.ReadLine();
            Console.Clear();

            if (pick == "1")
                pExam.ShowExam();
            else if (pick == "2")
                fExam.ShowExam();
            else
                Console.WriteLine("Invalid choice, try again.");
        }
    }
}