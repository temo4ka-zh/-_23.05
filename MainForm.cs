using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NotesReminderApp.Models;
using NotesReminderApp.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NotesApp
{
    public class MainForm : Form
    {
        private readonly NoteStorageService _storageService = new NoteStorageService();
        private readonly Logger _logger = new Logger();

        private List<Note> _allNotes = new List<Note>();
        private BindingList<Note> _visibleNotes = new BindingList<Note>();
        private BindingSource _bindingSource = new BindingSource();

        private DataGridView dgvNotes;
        private TextBox txtSearch;
        private ComboBox cmbFilter;

        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnDeleteCompleted;
        private Button btnSave;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        private Timer reminderTimer;

        public MainForm()
        {
            Text = "Менеджер заметок";
            Width = 1000;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;

            InitializeControls();
            InitializeGrid();
            InitializeTimer();

            LoadNotes();
            RefreshGrid();

            _logger.Log("Главная форма запущена");
        }

        private void InitializeControls()
        {
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60
            };

            Label lblSearch = new Label
            {
                Text = "Поиск:",
                Left = 10,
                Top = 20,
                Width = 50
            };

            txtSearch = new TextBox
            {
                Left = 65,
                Top = 15,
                Width = 220
            };
            txtSearch.TextChanged += (s, e) => RefreshGrid();

            Label lblFilter = new Label
            {
                Text = "Фильтр:",
                Left = 300,
                Top = 20,
                Width = 60
            };

            cmbFilter = new ComboBox
            {
                Left = 365,
                Top = 15,
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbFilter.Items.AddRange(new string[]
            {
                "Все",
                "Активные",
                "Выполненные",
                "Просроченные"
            });

            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (s, e) => RefreshGrid();

            btnAdd = new Button
            {
                Text = "Добавить",
                Left = 560,
                Top = 12,
                Width = 90
            };
            btnAdd.Click += BtnAdd_Click;

            btnEdit = new Button
            {
                Text = "Редактировать",
                Left = 655,
                Top = 12,
                Width = 120
            };
            btnEdit.Click += BtnEdit_Click;

            btnDelete = new Button
            {
                Text = "Удалить",
                Left = 780,
                Top = 12,
                Width = 90
            };
            btnDelete.Click += BtnDelete_Click;

            btnDeleteCompleted = new Button
            {
                Text = "Удалить выполненные",
                Left = 875,
                Top = 12,
                Width = 100
            };
            btnDeleteCompleted.Click += BtnDeleteCompleted_Click;

            topPanel.Controls.Add(lblSearch);
            topPanel.Controls.Add(txtSearch);
            topPanel.Controls.Add(lblFilter);
            topPanel.Controls.Add(cmbFilter);
            topPanel.Controls.Add(btnAdd);
            topPanel.Controls.Add(btnEdit);
            topPanel.Controls.Add(btnDelete);
            topPanel.Controls.Add(btnDeleteCompleted);

            dgvNotes = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };

            dgvNotes.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    EditSelectedNote();
            };

            dgvNotes.RowPrePaint += DgvNotes_RowPrePaint;

            statusStrip = new StatusStrip();

            statusLabel = new ToolStripStatusLabel
            {
                Text = "Готово"
            };

            statusStrip.Items.Add(statusLabel);

            Controls.Add(dgvNotes);
            Controls.Add(topPanel);
            Controls.Add(statusStrip);
        }

        private void InitializeGrid()
        {
            dgvNotes.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Заголовок",
                DataPropertyName = "Title",
                Width = 180
            });

            dgvNotes.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Текст",
                DataPropertyName = "Text",
                Width = 300
            });

            dgvNotes.Columns.Add(new DataGridViewCheckBoxColumn
            {
                HeaderText = "Выполнено",
                DataPropertyName = "IsCompleted",
                Width = 90
            });

            dgvNotes.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Дата создания",
                DataPropertyName = "CreatedAt",
                Width = 140,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "dd.MM.yyyy HH:mm"
                }
            });

            dgvNotes.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Напоминание",
                DataPropertyName = "ReminderAt",
                Width = 140,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "dd.MM.yyyy HH:mm"
                }
            });

            _bindingSource.DataSource = _visibleNotes;
            dgvNotes.DataSource = _bindingSource;
        }

        private void InitializeTimer()
        {
            reminderTimer = new Timer
            {
                Interval = 60_000
            };

            reminderTimer.Tick += ReminderTimer_Tick;
            reminderTimer.Start();
        }

        private void LoadNotes()
        {
            try
            {
                _allNotes = _storageService.LoadNotes();

                if (_allNotes == null)
                    _allNotes = new List<Note>();

                // Тестовые заметки, если файл пустой
                if (_allNotes.Count == 0)
                {
                    _allNotes.Add(new Note
                    {
                        Title = "Тестовая заметка",
                        Text = "Это пример заметки",
                        CreatedAt = DateTime.Now,
                        ReminderAt = DateTime.Now.AddMinutes(5),
                        IsCompleted = false
                    });

                    _allNotes.Add(new Note
                    {
                        Title = "Выполненная заметка",
                        Text = "Пример выполненной задачи",
                        CreatedAt = DateTime.Now,
                        ReminderAt = null,
                        IsCompleted = true
                    });
                }

                _logger.Log("Заметки загружены");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заметок: " + ex.Message);
                _logger.Log("Ошибка загрузки заметок: " + ex.Message);
                _allNotes = new List<Note>();
            }
        }

        private void SaveNotes()
        {
            try
            {
                _storageService.SaveNotes(_allNotes);
                statusLabel.Text = "Заметки сохранены";
                _logger.Log("Заметки сохранены");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения заметок: " + ex.Message);
                _logger.Log("Ошибка сохранения заметок: " + ex.Message);
            }
        }

        private void RefreshGrid()
        {
            string search = txtSearch.Text.Trim().ToLower();
            string filter = cmbFilter.SelectedItem?.ToString() ?? "Все";

            IEnumerable<Note> result = _allNotes;

            if (!string.IsNullOrWhiteSpace(search))
            {
                result = result.Where(n =>
                    (!string.IsNullOrEmpty(n.Title) && n.Title.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(n.Text) && n.Text.ToLower().Contains(search)));
            }

            if (filter == "Активные")
            {
                result = result.Where(n => !n.IsCompleted);
            }
            else if (filter == "Выполненные")
            {
                result = result.Where(n => n.IsCompleted);
            }
            else if (filter == "Просроченные")
            {
                result = result.Where(n =>
                    !n.IsCompleted &&
                    n.ReminderAt.HasValue &&
                    n.ReminderAt.Value < DateTime.Now);
            }

            _visibleNotes = new BindingList<Note>(result.ToList());
            _bindingSource.DataSource = _visibleNotes;

            statusLabel.Text = $"Заметок: {_visibleNotes.Count} из {_allNotes.Count}";
        }

        private Note GetSelectedNote()
        {
            if (dgvNotes.CurrentRow == null)
                return null;

            return dgvNotes.CurrentRow.DataBoundItem as Note;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using NoteForm form = new NoteForm();

            if (form.ShowDialog() == DialogResult.OK)
            {
                Note note = form.Note;

                if (note.CreatedAt == default)
                    note.CreatedAt = DateTime.Now;

                _allNotes.Add(note);

                _logger.Log("Добавлена заметка: " + note.Title);

                SaveNotes();
                RefreshGrid();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            EditSelectedNote();
        }

        private void EditSelectedNote()
        {
            Note selectedNote = GetSelectedNote();

            if (selectedNote == null)
            {
                MessageBox.Show("Выберите заметку для редактирования.");
                return;
            }

            Note copy = new Note
            {
                Title = selectedNote.Title,
                Text = selectedNote.Text,
                CreatedAt = selectedNote.CreatedAt,
                ReminderAt = selectedNote.ReminderAt,
                IsCompleted = selectedNote.IsCompleted
            };

            using NoteForm form = new NoteForm(copy);

            if (form.ShowDialog() == DialogResult.OK)
            {
                selectedNote.Title = form.Note.Title;
                selectedNote.Text = form.Note.Text;
                selectedNote.ReminderAt = form.Note.ReminderAt;
                selectedNote.IsCompleted = form.Note.IsCompleted;

                _logger.Log("Отредактирована заметка: " + selectedNote.Title);

                SaveNotes();
                RefreshGrid();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            Note selectedNote = GetSelectedNote();

            if (selectedNote == null)
            {
                MessageBox.Show("Выберите заметку для удаления.");
                return;
            }

            DialogResult result = MessageBox.Show(
                "Удалить выбранную заметку?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _allNotes.Remove(selectedNote);

                _logger.Log("Удалена заметка: " + selectedNote.Title);

                SaveNotes();
                RefreshGrid();
            }
        }

        private void BtnDeleteCompleted_Click(object sender, EventArgs e)
        {
            int count = _allNotes.Count(n => n.IsCompleted);

            if (count == 0)
            {
                MessageBox.Show("Нет выполненных заметок.");
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Удалить выполненные заметки? Количество: {count}",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _allNotes.RemoveAll(n => n.IsCompleted);

                _logger.Log("Удалены выполненные заметки. Количество: " + count);

                SaveNotes();
                RefreshGrid();
            }
        }

        private void ReminderTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            foreach (Note note in _allNotes)
            {
                if (!note.IsCompleted &&
                    note.ReminderAt.HasValue &&
                    note.ReminderAt.Value <= now &&
                    note.ReminderAt.Value > now.AddMinutes(-1))
                {
                    MessageBox.Show(
                        note.Text,
                        "Напоминание: " + note.Title,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    _logger.Log("Показано напоминание: " + note.Title);
                }
            }
        }

        private void DgvNotes_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            Note note = dgvNotes.Rows[e.RowIndex].DataBoundItem as Note;

            if (note == null)
                return;

            if (note.IsCompleted)
            {
                dgvNotes.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
            }
            else if (note.ReminderAt.HasValue && note.ReminderAt.Value < DateTime.Now)
            {
                dgvNotes.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
            }
            else if (note.ReminderAt.HasValue && note.ReminderAt.Value <= DateTime.Now.AddHours(1))
            {
                dgvNotes.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightYellow;
            }
            else
            {
                dgvNotes.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveNotes();
            _logger.Log("Главная форма закрыта");
            base.OnFormClosing(e);
        }
    }
}
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