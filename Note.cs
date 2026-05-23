using System;

namespace NotesReminderApp.Models
{
    public class Note
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Title { get; set; } = string.Empty;

        public DateTime ReminderDateTime { get; set; } = DateTime.Now.AddMinutes(10);

        public string Priority { get; set; } = "Средний";

        public bool IsCompleted { get; set; } = false;

        public string Description { get; set; } = string.Empty;

        public bool ReminderShown { get; set; } = false;

        // Для отображения в DataGridView
        public string ReminderText
        {
            get
            {
                return ReminderDateTime.ToString("dd.MM.yyyy HH:mm");
            }
        }

        // Для отображения статуса в DataGridView
        public string StatusText
        {
            get
            {
                return IsCompleted ? "Выполнено" : "Не выполнено";
            }
        }

        // Для записи в лог
        public string ToLogString()
        {
            return $"Название: {Title}; Дата: {ReminderDateTime:dd.MM.yyyy HH:mm}; " +
                   $"Приоритет: {Priority}; Статус: {StatusText}; Описание: {Description}";
        }
    }
}