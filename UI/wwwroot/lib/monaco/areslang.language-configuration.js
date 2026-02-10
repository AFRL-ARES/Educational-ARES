export const conf = {
  comments: {
    lineComment: '#',
  },

  brackets: [
    ['{', '}'],
    ['[', ']'],
    ['(', ')'],
  ],

  autoClosingPairs: [
    { open: '{', close: '}' },
    { open: '[', close: ']' },
    { open: '(', close: ')' },
    { open: '"', close: '"', notIn: ['string'] },
    { open: "'", close: "'", notIn: ['string'] },
  ],

  surroundingPairs: [
    { open: '{', close: '}' },
    { open: '[', close: ']' },
    { open: '(', close: ')' },
    { open: '"', close: '"' },
    { open: "'", close: "'" },
  ],

  indentationRules: {
    increaseIndentPattern: /^\s*(?:if|elif|else|for|while|def|parallel)\b.*:\s*$/,
    decreaseIndentPattern: /^\s*(?:elif|else)\b.*:\s*$/,
  },

  folding: {
    offSide: true,
  },
};
