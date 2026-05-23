using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NotesReminderApp.Models;
using NotesReminderApp.Services;

namespace NotesReminderApp
{
    public partial class MainForm : Form
    {
        private List<Note> _notes = new List<Note>();
        private BindingSource _bindingSource = new BindingSource();

        private readonly string _dataFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data",
            "notes.json"
        );

        private NoteStorageService _storageService;

        public MainForm()
        {
            InitializeComponent();

            _storageService = new NoteStorageService(_dataFilePath);

            Load += MainForm_Load;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            Logger.Log("Запуск приложения");

            ConfigureDataGridView();
            ConfigureFilters();
            LoadNotesFromFile();
            RefreshNotesGrid();
            UpdateStatusBar();
        }

        private void ConfigureDataGridView()
        {
            dgvNotes.AutoGenerateColumns = false;
            dgvNotes.Columns.Clear();

            dgvNotes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvNotes.MultiSelect = false;
            dgvNotes.ReadOnly = true;
            dgvNotes.AllowUserToAddRows = false;
            dgvNotes.AllowUserToDeleteRows = false;
            dgvNotes.RowHeadersVisible = false;

            DataGridViewTextBoxColumn titleColumn = new DataGridViewTextBoxColumn();
            titleColumn.HeaderText = "Название заметки";
            titleColumn.DataPropertyName = "Title";
            titleColumn.Width = 220;

            DataGridViewTextBoxColumn reminderColumn = new DataGridViewTextBoxColumn();
            reminderColumn.HeaderText = "Дата и время напоминания";
            reminderColumn.DataPropertyName = "ReminderText";
            reminderColumn.Width = 180;

            DataGridViewTextBoxColumn priorityColumn = new DataGridViewTextBoxColumn();
            priorityColumn.HeaderText = "Приоритет";
            priorityColumn.DataPropertyName = "Priority";
            priorityColumn.Width = 120;

            DataGridViewTextBoxColumn statusColumn = new DataGridViewTextBoxColumn();
            statusColumn.HeaderText = "Статус";
            statusColumn.DataPropertyName = "StatusText";
            statusColumn.Width = 130;

            dgvNotes.Columns.Add(titleColumn);
            dgvNotes.Columns.Add(reminderColumn);
            dgvNotes.Columns.Add(priorityColumn);
            dgvNotes.Columns.Add(statusColumn);

            dgvNotes.DataSource = _bindingSource;
        }

        private void ConfigureFilters()
        {
            cmbPriorityFilter.Items.Clear();

            cmbPriorityFilter.Items.Add("Все");
            cmbPriorityFilter.Items.Add("Высокий");
            cmbPriorityFilter.Items.Add("Средний");
            cmbPriorityFilter.Items.Add("Низкий");

            cmbPriorityFilter.SelectedIndex = 0;

            cmbPriorityFilter.SelectedIndexChanged += FilterChanged;
            chkOnlyUncompleted.CheckedChanged += FilterChanged;
            txtSearch.TextChanged += FilterChanged;

            btnRefresh.Click += BtnRefresh_Click;
        }

        private void LoadNotesFromFile()
        {
            _notes = _storageService.LoadNotes();
        }

        private void RefreshNotesGrid()
        {
            IEnumerable<Note> filteredNotes = _notes;

            string? selectedPriority = cmbPriorityFilter.SelectedItem?.ToString();

            if (!string.IsNullOrWhiteSpace(selectedPriority) && selectedPriority != "Все")
            {
                filteredNotes = filteredNotes.Where(note => note.Priority == selectedPriority);
            }

            if (chkOnlyUncompleted.Checked)
            {
                filteredNotes = filteredNotes.Where(note => !note.IsCompleted);
            }

            string searchText = txtSearch.Text.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filteredNotes = filteredNotes.Where(note =>
                    note.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            List<Note> result = filteredNotes
                .OrderBy(note => note.IsCompleted)
                .ThenBy(note => note.ReminderDateTime)
                .ToList();

            _bindingSource.DataSource = result;

            ApplyUrgencyColors();
            UpdateStatusBar();
        }

        private void ApplyUrgencyColors()
        {
            foreach (DataGridViewRow row in dgvNotes.Rows)
            {
                if (row.DataBoundItem is not Note note)
                {
                    continue;
                }

                if (note.IsCompleted)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGray;
                }
                else if (note.ReminderDateTime < DateTime.Now)
                {
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
                }
                else if (note.ReminderDateTime <= DateTime.Now.AddHours(1))
                {
                    row.DefaultCellStyle.BackColor = Color.LightSalmon;
                }
                else if (note.ReminderDateTime <= DateTime.Now.AddDays(1))
                {
                    row.DefaultCellStyle.BackColor = Color.LightYellow;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                }
            }
        }

        private void UpdateStatusBar()
        {
            lblCurrentTime.Text = $"Время: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
            lblNotesCount.Text = $"Заметок: {_notes.Count}";
            lblFilePath.Text = $"Файл: {_dataFilePath}";
        }

        private void FilterChanged(object? sender, EventArgs e)
        {
            RefreshNotesGrid();
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadNotesFromFile();

            Logger.Log("Список заметок обновлён из файла");

            RefreshNotesGrid();

            MessageBox.Show(
                "Список заметок обновлён.",
                "Обновление",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }
}