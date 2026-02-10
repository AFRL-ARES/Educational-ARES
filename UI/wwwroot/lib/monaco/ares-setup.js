import { language } from './areslang.monarch.js';
import { conf } from './areslang.language-configuration.js';

/**
 * Registers the ARES language with Monaco Editor.
 * This should be called once before the editor is initialized with the 'ares' language.
 */
export function registerAresLanguage() {
  if (typeof monaco === 'undefined') {
    console.error('Monaco Editor is not loaded. Ensure BlazorMonaco is properly initialized.');
    return;
  }

  // Register the language ID if it hasn't been registered yet
  const languages = monaco.languages.getLanguages();
  if (!languages.some(lang => lang.id === 'ares')) {
    monaco.languages.register({ id: 'ares' });
  }

  // Set the Monarch tokens provider
  monaco.languages.setMonarchTokensProvider('ares', language);

  // Set the language configuration
  monaco.languages.setLanguageConfiguration('ares', conf);

  monaco.editor.defineTheme('ares-dark', {
    base: 'vs-dark',
    inherit: true,
    rules: [
      { token: 'identifier.function', foreground: '#DCDCAA' },
      { token: 'keyword.flow', foreground: '#C586C0' },
      { token: 'keyword.special', foreground: '#CE9178' }
    ],
    colors: {}
  });
  
  monaco.editor.setTheme('ares-dark');
  
  console.log("Apparently set the theme");
}
