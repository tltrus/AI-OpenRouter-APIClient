using AI.Commands;
using AI.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private const string ApiKey = "!!!!!!!!!!!!!!!!!!GENERATE YOUR OWN KEY!!!!!!!!!!!!!!!!!!!!!!";
        private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";
        private static readonly HttpClient client = new HttpClient();

        private string _inputText;
        private string _outputText;
        private OpenRouterModel _selectedModel;
        private bool _showFreeOnly = true;
        private bool _isBusy;
        private string _selectedModelInfo;
        private bool _modelsLoaded = false;

        public event EventHandler<string> ResponseUpdated;

        public ObservableCollection<OpenRouterModel> Models { get; } = new ObservableCollection<OpenRouterModel>();
        public ObservableCollection<OpenRouterModel> FilteredModels { get; } = new ObservableCollection<OpenRouterModel>();

        public ICommand SendCommand { get; }
        public ICommand GetModelsCommand { get; }
        public ICommand ToggleContextMemoryCommand { get; }
        public ICommand ToggleHistoryCommand { get; }

        public MainViewModel()
        {
            SendCommand = new RelayCommand(async () => await SendRequest(), () => !IsBusy && !string.IsNullOrWhiteSpace(InputText) && SelectedModel != null);
            GetModelsCommand = new RelayCommand(async () => await LoadModels(), () => !IsBusy);

            // Инициализируем тестовыми моделями при старте
            InitializeTestModels();
        }

        private void InitializeTestModels()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Models.Clear();
                Models.Add(new OpenRouterModel
                {
                    Id = "qwen/qwen3-coder:free",
                    Name = "Qwen Coder 7B",
                    Description = "Specialized programming model from Alibaba Cloud",
                    IsFree = true,
                    Provider = "Alibaba Cloud",
                    ContextLength = "8K tokens",
                    PricingInfo = "Free"
                });

                Models.Add(new OpenRouterModel
                {
                    Id = "mistralai/mistral-7b-instruct:free",
                    Name = "Mistral 7B Instruct",
                    Description = "High-performance 7B parameter model from Mistral AI",
                    IsFree = true,
                    Provider = "Mistral AI",
                    ContextLength = "32K tokens",
                    PricingInfo = "Free"
                });

                Models.Add(new OpenRouterModel
                {
                    Id = "openai/gpt-3.5-turbo",
                    Name = "GPT-3.5 Turbo",
                    Description = "Fast and efficient model from OpenAI",
                    IsFree = false,
                    Provider = "OpenAI",
                    ContextLength = "16K tokens",
                    PricingInfo = "Paid"
                });

                ApplyFilter();

                // Выбираем первую модель по умолчанию
                if (FilteredModels.Count > 0)
                {
                    SelectedModel = FilteredModels.FirstOrDefault();
                }
            });
        }



        public string InputText
        {
            get => _inputText;
            set
            {
                _inputText = value;
                OnPropertyChanged();
            }
        }


        public string OutputText
        {
            get => _outputText;
            set
            {
                _outputText = value;
                OnPropertyChanged();

                // Вызываем событие для обновления UI
                ResponseUpdated?.Invoke(this, value);
            }
        }


        public OpenRouterModel SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (_selectedModel != value)
                {
                    // Проверяем, нужно ли очищать историю
                    bool shouldClearHistory = _selectedModel != null &&
                                              value != null &&
                                              _selectedModel.Id != value.Id;

                    _selectedModel = value;
                    OnPropertyChanged();
                    UpdateSelectedModelInfo();
                }
            }
        }

        public string SelectedModelInfo
        {
            get => _selectedModelInfo;
            set
            {
                _selectedModelInfo = value;
                OnPropertyChanged();
            }
        }

        public bool ShowFreeOnly
        {
            get => _showFreeOnly;
            set
            {
                _showFreeOnly = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
                ((RelayCommand)GetModelsCommand).RaiseCanExecuteChanged();
            }
        }

        private void UpdateSelectedModelInfo()
        {
            if (SelectedModel != null)
            {
                SelectedModelInfo = $"Selected: {SelectedModel.Name}";
            }
            else
            {
                SelectedModelInfo = "No model selected";
            }
        }

        private async Task SendRequest()
        {
            if (SelectedModel == null || string.IsNullOrWhiteSpace(InputText))
            {
                OutputText = "Please select a model and enter a query";
                return;
            }

            IsBusy = true;
            // ОЧИЩАЕМ предыдущий ответ перед отправкой нового запроса
            OutputText = "Loading...";

            try
            {
                // Подготавливаем сообщения для отправки
                var messages = new List<object>();

                // Отправляем только текущее сообщение
                messages.Add(new { role = "user", content = InputText });

                var requestData = new
                {
                    model = SelectedModel.Id,
                    messages = messages,
                    temperature = 0.7
                };

                string json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
                client.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost:8080");
                client.DefaultRequestHeaders.Add("X-Title", "OpenRouter WPF App");

                HttpResponseMessage response = await client.PostAsync(ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonConvert.DeserializeObject<OpenRouterResponse>(responseContent);
                    var aiResponse = responseObject?.choices[0]?.message.content;

                    // Обновляем OutputText - это вызовет FormatResponse
                    OutputText = aiResponse;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var errorObject = JsonConvert.DeserializeObject<dynamic>(errorContent);
                        var errorMessage = errorObject?.error?.message ?? errorContent;
                        OutputText = $"Error {response.StatusCode}: {errorMessage}";
                    }
                    catch
                    {
                        OutputText = $"Error {response.StatusCode}: {errorContent}";
                    }
                }
            }
            catch (Exception ex)
            {
                OutputText = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadModels()
        {
            if (_modelsLoaded && Models.Count > 0)
            {
                OutputText = "Models already loaded";
                return;
            }

            IsBusy = true;
            OutputText = "Loading model list...";

            try
            {
                string apiUrl = "https://openrouter.ai/api/v1/models";

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
                client.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost:8080");
                client.DefaultRequestHeaders.Add("X-Title", "OpenRouter WPF App");

                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        // Десериализуем ответ
                        var modelsResponse = JsonConvert.DeserializeObject<OpenRouterModelsResponse>(responseContent);

                        // Обновляем UI в основном потоке
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            UpdateModelsList(modelsResponse.Data);
                            _modelsLoaded = true;
                        });
                    }
                    catch (JsonException jsonEx)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            OutputText = $"JSON parsing error: {jsonEx.Message}\nUsing demo models";
                            InitializeTestModels();
                        });
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        OutputText = $"Load error: {response.StatusCode}\n{errorContent}\nUsing demo models";
                        InitializeTestModels();
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    OutputText = $"Error: {ex.Message}\nUsing demo models";
                    InitializeTestModels();
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateModelsList(List<OpenRouterModelInfo> apiModels)
        {
            Models.Clear();

            // Добавляем модели из API
            foreach (var apiModel in apiModels)
            {
                var model = OpenRouterModel.FromApiModel(apiModel);
                if (model != null)
                {
                    Models.Add(model);
                }
            }

            // Применяем фильтр
            ApplyFilter();

            OutputText = $"Loaded {Models.Count} models";

            // Автоматически выбираем первую модель
            if (FilteredModels.Count > 0)
            {
                SelectedModel = FilteredModels.FirstOrDefault();
            }
        }

        private void ApplyFilter()
        {
            FilteredModels.Clear();
            var filtered = ShowFreeOnly ?
                Models.Where(m => m.IsFree) :
                Models;

            foreach (var model in filtered)
            {
                FilteredModels.Add(model);
            }

            if (SelectedModel != null && !FilteredModels.Contains(SelectedModel))
            {
                SelectedModel = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}