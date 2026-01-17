# AI-OpenRouter-APIClient

A modern, feature-rich WPF desktop application for interacting with AI models through the OpenRouter API.

✨ Features
🎯 Multi-Model Support: Access various AI models including free and paid options

💬 Real-time Chat: Interactive chat interface with markdown formatting support

📝 Rich Text Display: Beautifully formatted responses with:

Code highlighting for C#, Python, JavaScript

Markdown support (headers, lists, bold, inline code)

Syntax-aware formatting with custom highlighter

⚡ Smart Input: Send messages with Enter, new lines with Shift+Enter

🔍 Model Browser: Filter models by provider, price, and capabilities

📊 Detailed Model Info: View pricing, context length, and descriptions

🚀 Performance Optimized: Pre-compiled regex, efficient text processing

🛠️ Technologies
.NET Framework / .NET Core: WPF desktop application

MVVM Pattern: Clean architecture with ViewModel separation

OpenRouter API: Integration with multiple AI providers

Modern WPF Styling: Custom controls with hover effects and animations

Async/Await: Non-blocking API calls with cancellation support

📦 Installation
Clone the repository

bash
git clone https://github.com/yourusername/openrouter-wpf-client.git
Open in Visual Studio

Open the .sln file

Restore NuGet packages

Configure API Key

Get your API key from OpenRouter

Replace the API key in MainViewModel.cs

Build and Run

Press F5 to start debugging

Or build and run the executable

🚀 Quick Start
Launch the application

Select a model from the left panel

Type your message in the bottom text box

Press Enter to send

Use Shift+Enter for multi-line input

🔧 Configuration
Model Selection
Browse available models with the "Refresh Model List" button

Filter to show only free models with the checkbox

View detailed information about each model

Customization
Modify CSharpSyntaxHighlighter.cs to add support for more programming languages

Adjust formatting colors in the syntax highlighter

Customize UI styles in XAML files

📁 Project Structure
text
OpenRouterClient/
├── View/
│   ├── MainWindow.xaml          # Main application window
│   └── MainWindow.xaml.cs       # Window code-behind
├── ViewModels/
│   └── MainViewModel.cs         # Main ViewModel with business logic
├── Models/
│   ├── OpenRouterModel.cs       # Model data classes
│   └── OpenRouterResponse.cs    # API response classes
├── Utils/
│   └── CSharpSyntaxHighlighter.cs # Code syntax highlighting
├── Converters/
│   ├── FreeToColorConverter.cs  # UI value converters
│   └── InverseBooleanConverter.cs
└── Commands/
    └── RelayCommand.cs          # Command pattern implementation
💡 Usage Tips
Code Blocks: Use triple backticks (```) for code blocks

Bold Text: Use double asterisks (**bold**)

Inline Code: Use single backticks (code)

Lists: Use -, *, or + for bullet lists, 1. for numbered lists

Headers: Use ### for section headers

🤝 Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

Fork the repository

Create your feature branch (git checkout -b feature/AmazingFeature)

Commit your changes (git commit -m 'Add some AmazingFeature')

Push to the branch (git push origin feature/AmazingFeature)

Open a Pull Request

📄 License
This project is licensed under the MIT License - see the LICENSE file for details.

🙏 Acknowledgments
OpenRouter for providing API access to multiple AI models

All AI model providers available through OpenRouter

The WPF and .NET communities for excellent tools and libraries

📞 Support
For issues and feature requests, please use the GitHub Issues page.

Note: This application requires an internet connection and a valid OpenRouter API key. Some models may have usage limits or costs associated with them.
