using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NotesReminderApp.Models;
namespace NotesReminderApp.Services
{
    public class NoteStorageService
    {
        private readonly string _filePath;

        public NoteStorageService(string filePath)
        {
            _filePath = filePath;
        }

        public List<Note> LoadNotes()
        {
            try
            {
                string? directory = Path.GetDirectoryName(_filePath);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(_filePath))
                {
                    Logger.Log("Файл данных не найден. Создан пустой список заметок.");
                    return new List<Note>();
                }

                string json = File.ReadAllText(_filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Log("Файл данных пустой. Загружен пустой список заметок.");
                    return new List<Note>();
                }

                List<Note>? notes = JsonSerializer.Deserialize<List<Note>>(json);

                Logger.Log($"Данные загружены из файла. Количество заметок: {notes?.Count ?? 0}");

                return notes ?? new List<Note>();
            }
            catch (Exception ex)
            {
                Logger.Log($"Ошибка загрузки данных: {ex.Message}");
                return new List<Note>();
            }
        }
        public void SaveNotes(List<Note> notes)
        {
            try
            {
                string? directory = Path.GetDirectoryName(_filePath);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(notes, options);

                File.WriteAllText(_filePath, json);

                Logger.Log($"Данные сохранены в файл. Количество заметок: {notes.Count}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Ошибка сохранения данных: {ex.Message}");
                throw;
            }
        }
    }
}