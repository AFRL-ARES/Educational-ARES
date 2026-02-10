# Monaco Editor ARES Language Integration

This directory contains the Monaco Editor language definition for ARES.

## Integration with BlazorMonaco

To use this language in a Blazor project using the `BlazorMonaco` package, follow these steps:

1. **Serve the files**: Ensure the files in this directory are available in your `wwwroot` folder (e.g., `wwwroot/monaco/`).
2. **Register the language**: Import and call the registration function in your Razor component.

### Example Razor Component Usage

```csharp
@inject IJSRuntime JSRuntime

<MonacoEditor @ref="_editor" Id="ares-editor" ConstructionOptions="EditorConstructionOptions" />

@code {
    private MonacoEditor _editor;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Import the setup script as a JS Module
            // Ensure the path matches your actual file structure in wwwroot
            var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./monaco/ares-setup.js");
            
            // Register the language with Monaco
            await module.InvokeVoidAsync("registerAresLanguage");
        }
    }

    private StandaloneEditorConstructionOptions EditorConstructionOptions(MonacoEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            Language = "ares",
            Theme = "vs-dark",
            AutomaticLayout = true,
            Value = "# Welcome to ARES\ndef main():\n    return True\n"
        };
    }
}
```

## Files
- `areslang.monarch.js`: Tokenization rules (syntax highlighting).
- `areslang.language-configuration.js`: Language features (brackets, comments, indentation).
- `ares-setup.js`: Glue script to register the language with the Monaco global instance.
