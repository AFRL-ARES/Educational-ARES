grammar AresLang;

// Virtual tokens for indentation (AresIndentationLexer takes care of these) These are necessary to
// achieve that python-like feel everybody loves
tokens {
	INDENT,
	DEDENT
}

@members {
    private int nestingLevel = 0;
    private int funcDepth = 0;
    private int loopDepth = 0;
}

// --- Parser Rules (Syntax) ---

// The entry point: a program is a list of top-level statements ending with End-Of-File
program: (statement | NEWLINE)* EOF;

// Statements (context-validated via predicates)
statement:
	simpleStatement								# SimpleStmt
	| ifStatement								  # IfStmt
	| whileStatement							# WhileStmt
	| forStatement								# ForStmt
	| {loopDepth > 0}? loopControlStatement		# LoopControlStmt
	| {funcDepth > 0}? funcControlStatement		# FuncControlStmt
	| parallelStatement           # ParallelStmt;

// Simple statements. Pretty much just statements with no direct loops
simpleStatement:
  assignment terminator	# AssignStmt
  | expression terminator	# ExprStmt
  | assertStatement terminator # AssertStmt
  | functionDeclaration	# FunctionDecl;

// These are specifically meant to control the flow of loops like we can break out of a function or
// continue to the next iteration
loopControlStatement:
	BREAK terminator		# BreakStmt
	| CONTINUE terminator	# ContinueStmt;

// Functions can return values using the return statement
funcControlStatement:
  RETURN expression? terminator # ReturnStmt;

terminator: NEWLINE+ | EOF;

// Assert takes in an expression to be tested and an optional output expression that will act as the exception message
// in case assertion fails. Ex.: assert 2 + 2 == 5, "Should've been 4 :)"
assertStatement:
  ASSERT expression (',' expression)?;

ifStatement:
	IF expression COLON block (ELIF expression COLON block)* (
		ELSE COLON block
	)?;

whileStatement:
	WHILE expression COLON loopBlock;

forStatement:
	FOR ID IN expression COLON loopBlock;
	
parallelStatement:
  PARALLEL COLON parallelBlock;

// Simple block for non-loop statements (no loop depth change)
block: NEWLINE INDENT (statement NEWLINE*)+ DEDENT;

// Loop block increments/decrements loop depth for break/continue validation
loopBlock:
	NEWLINE INDENT {loopDepth++;} (statement NEWLINE*)+ DEDENT {loopDepth--;};

// Function declarations. TODO: Decide if AresScript should even support custom functions
functionDeclaration:
	DEF ID LPAREN (ID (',' ID)*)? RPAREN COLON funcBlock;

// Function body increments/decrements func depth for return validation
funcBlock:
	NEWLINE INDENT {funcDepth++;} (statement NEWLINE*)+ DEDENT {funcDepth--;};

// Parallel block executes expression asynchronously. Let's not worry about statements for now
parallelBlock:
  NEWLINE INDENT (expression NEWLINE*)+ DEDENT;
  
// Assignment statements
assignment: lvalue '=' expression;

// lvalue defined specifically for assignments as assigning to rvalues is kinda silly
lvalue:
	ID							        # LValueId
	| lvalue '.' ID				        # LValueMember
	| lvalue LBRACK expression RBRACK	# LValueIndex;

// Expressions!!!
expression:
	expression '.' ID										# MemberAccess
	| expression LBRACK expression RBRACK							# IndexAccess
	| expression LPAREN argList? RPAREN	# FunctionCall
	| SUB expression										# UnaryMinus
	| expression op = (MUL | DIV | MOD) expression			# MulDiv
	| expression op = (ADD | SUB) expression				# AddSub
	| expression op = (GT | LT | GE | LE) expression		# Relational
	| expression op = (EQ | NEQ) expression					# Equality
	| NOT expression										# LogicalNot
	| expression AND expression								# LogicAnd
	| expression OR expression								# LogicOr
	| atom													# AtomExpr;

// Function-call argument list. Supports positional args and keyword args like python.
argList: argument (',' argument)* ','?;

argument:
	ID '=' expression # KeywordArg
	| expression      # PositionalArg;

// Basic atoms: literals, identifiers, parenthesized expressions, arrays, objects
atom:
	INT											# Int
	| FLOAT										# Float
	| STRING									# String
	| BOOL										# Bool
	| NONE										# None
	| ID										# Id
	| LPAREN expression RPAREN						# Parens
	| LBRACK (expression (',' expression)*)? RBRACK	# Array
	| obj										# Object;

// Key-Value pairs for objects. Python can apparently support expressions as keys, but that seems a
// bit overkill for ARES scripts, so we'll limit it to just identifiers and strings for now
pair: (ID | STRING) ':' expression;

// JSON-like struct definition TODO: Decide if we should actually support structs in AresScript
obj: LBRACE (pair ((',' pair))*)? RBRACE;

// --- Lexer Rules (Tokens) ---

// 1. Increment nesting on opening symbols
LPAREN : '(' { nestingLevel++; };
LBRACK : '[' { nestingLevel++; };
LBRACE : '{' { nestingLevel++; };

// 2. Decrement nesting on closing symbols
RPAREN : ')' { nestingLevel--; };
RBRACK : ']' { nestingLevel--; };
RBRACE : '}' { nestingLevel--; };

MUL: '*';
DIV: '/';
MOD: '%';
ADD: '+';
SUB: '-';

EQ: '==';
NEQ: '!=';
GT: '>';
LT: '<';
GE: '>=';
LE: '<=';

// Keywords
IF: 'if';
ELSE: 'else';
ELIF: 'elif';
WHILE: 'while';
FOR: 'for';
IN: 'in';
DEF: 'def';
RETURN: 'return';
BREAK: 'break';
CONTINUE: 'continue';
ASSERT: 'assert';
AND: 'and';
OR: 'or';
NOT: 'not';
COLON: ':';
PARALLEL: 'parallel';

// Boolean literals
BOOL: 'True' | 'False';

// None literal
NONE: 'None';

// Identifiers (variable names): starts with letter/underscore, followed by alphanumeric
ID: [a-zA-Z_] [a-zA-Z0-9_]*;

// Integers: one or more digits
INT: DIGIT+;

// Float: floating point numbers (actually doubles behind the scenes, but float sounds cool)
FLOAT: DIGIT+ '.' DIGIT+;

fragment DIGIT: [0-9];

// Whitespace: skip spaces and tabs so the parser ignores them
WS: [ \t]+ -> skip;

NEWLINE: 
    ('\r'? '\n' | '\r')+
    {
        // If we are inside brackets, throw this token away
        // effectively ignoring the newline.
        if (nestingLevel > 0) 
            Skip();
        
        // Otherwise, it's a real terminator.
    }
    ;

// String: double or single quoted characters. Support for escape sequences as well
STRING: ('"' ( '\\' . | ~["\\\r\n])* '"')
	| ('\'' ( '\\' . | ~['\\\r\n])* '\'');

// Comments: skip single line comments starting with # just like python :)
COMMENT: '#' ~[\r\n]* -> skip;
