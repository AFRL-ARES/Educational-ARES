export const language = {
  defaultToken: '',
  tokenPostfix: '.ares',

  keywords: [
    'in',
    'def',
    'and',
    'or',
    'not',
  ],

  flowKeywords: [
    'return',
    'break',
    'continue',
    'if',
    'else',
    'elif',
    'while',
    'for',
    'parallel',
  ],

  specialKeywords: [
    'assert'
  ],

  constants: ['True', 'False', 'None'],

  operators: [
    '=',
    '==',
    '!=',
    '>',
    '<',
    '>=',
    '<=',
    '+',
    '-',
    '*',
    '/',
    '%',
  ],

  symbols: /[=><!+\-*\/\%]+/,

  escapes: /\\(?:[abfnrtv\\"'0-9xua-fA-F])/, // Keep broad to allow typical escapes.

  brackets: [
    { open: '{', close: '}', token: 'delimiter.curly' },
    { open: '[', close: ']', token: 'delimiter.square' },
    { open: '(', close: ')', token: 'delimiter.parenthesis' },
  ],

  tokenizer: {
    root: [
      { include: '@whitespace' },

      [/[{}()[\]]/, '@brackets'],
      [/[,:.]/, 'delimiter'],

      [/\d+\.\d+/, 'number.float'],
      [/\d+/, 'number'],

      [/(def)(\s+)([a-zA-Z_][a-zA-Z0-9_]*)/, ['keyword', '', 'identifier.function']],

      [/[a-zA-Z_][a-zA-Z0-9_]*/, {
        cases: {
          '@keywords': 'keyword',
          '@constants': 'constant',
          '@flowKeywords': 'keyword.flow',
          '@specialKeywords': 'keyword.special',
          '@default': 'identifier',
        },
      }],

      [/@symbols/, {
        cases: {
          '@operators': 'operator',
          '@default': 'delimiter',
        },
      }],

      [/"([^"\\]|\\.)*$/, 'string.invalid'],
      [/'([^'\\]|\\.)*$/, 'string.invalid'],
      [/"/, { token: 'string.quote', bracket: '@open', next: '@string_double' }],
      [/'/, { token: 'string.quote', bracket: '@open', next: '@string_single' }],
    ],

    whitespace: [
      [/[ \t\r\n]+/, ''],
      [/#.*$/, 'comment'],
    ],

    string_double: [
      [/[^\\"]+/, 'string'],
      [/@escapes/, 'string.escape'],
      [/\\./, 'string.escape.invalid'],
      [/"/, { token: 'string.quote', bracket: '@close', next: '@pop' }],
    ],

    string_single: [
      [/[^\\']+/, 'string'],
      [/@escapes/, 'string.escape'],
      [/\\./, 'string.escape.invalid'],
      [/'/, { token: 'string.quote', bracket: '@close', next: '@pop' }],
    ],
  },
};
