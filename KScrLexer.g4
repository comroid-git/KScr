lexer grammar KScrLexer;

TRY: 'try';
CATCH: 'catch';
FINALLY: 'finally';

PUBLIC: 'public';
INTERNAL: 'internal';
PROTECTED: 'protected';
PRIVATE: 'private';

SYNCHRONIZED: 'synchronized';
ABSTRACT: 'abstract';
NATIVE: 'native';
STATIC: 'static';
FINAL: 'final';

STRICTFP: 'strictfp';
VOLATILE: 'volatile';

INSTANCEOF: 'instanceof';
RETURN: 'return';
ASSERT: 'assert';
NEW: 'new';
THROW: 'throw';
GET: 'get';
SET: 'set';

CLASS: 'class';
INTERFACE: 'interface';
ENUM: 'enum';
RECORD: 'record';
SINGLE: 'single';
ANNOTATION: 'annotation';

IMPORT: 'import';
PACKAGE: 'package';
EXTENDS: 'extends';
IMPLEMENTS: 'implements';

THIS: 'this';
SUPER: 'super';

SEALED: 'sealed';
PERMITS: 'permits';
NONSEALED: 'non-sealed';

DEFAULT: 'default';
SWITCH: 'switch';
WHILE: 'while';
YIELD: 'yield';
CASE: 'case';
ELSE: 'else';
FOREACH: 'foreach';
FOR: 'for';
DO: 'do';
IF: 'if';

NUMLIT: MINUS? DIGIT+ ((DOT DIGIT+ ('f'? | 'd'))? | ('l' | 'i' | 's' | 'b')?);
STRLIT: QUOTE (ESCAPE_QUOTE | (~[\r\n"]))*? (QUOTE);
BOOLLIT: TRUE | FALSE;
STDIOLIT: 'stdio';
NULL: 'null';

AND: '&&';
OR: '||';
PLUSPLUS: '++';
MINUSMINUS: '--';

BITAND: '&';
BITOR: '|';
UP: '^';

STAR: '*';
SLASH: '/';
PLUS: '+';
MINUS: '-';
PERCENT: '%';
AT: '@';

LSHIFT: '<<';
RSHIFT: '>>';
ULSHIFT: '<<<';
URSHIFT: '>>>';

EQUAL: '==';
INEQUAL: '!=';
GREATEREQ: '>=';
LESSEREQ: '<=';
GREATER: '>';
LESSER: '<';

ASSIGN: '=';

LBRACE: '{';
RBRACE: '}';
LPAREN: '(';
RPAREN: ')';
LSQUAR: '[';
RSQUAR: ']';

COLON: ':';
SEMICOLON: ';';
DOT: '.';
COMMA: ',';
EXCLAMATION: '!';
QUESTION: '?';
ELIPSES: '...';
TILDE: '~';

ESCAPE_QUOTE: '\\"';
QUOTE: '"';

LDASHARROW: '<-';
RDASHARROW: '->';
DDASHARROW: '<->';
REQARROW: '=>';
DEQARROW: '<=>';
LBOXARROW: '<|';
RBOXARROW: '|>';
DBOXARROW: '<|>';
LENDARROW: '-|';
RENDARROW: '|-';

OBJECT: 'object';
ARRAYIDENT: 'array';
TUPLEIDENT: 'tuple';
TYPE: 'type';
NUMIDENT: 'num';
BYTE: 'byte';
SHORT: 'short';
INT: 'int';
LONG: 'long';
FLOAT: 'float';
DOUBLE: 'double';
VOID: 'void';

VAR: 'var';

TRUE: 'true';
FALSE: 'false';

ID: NONDIGIT (DIGIT | NONDIGIT)*;
DIGIT: [0-9];
NONDIGIT: [a-zA-Z_$£#];

SING_COMMENT: '//' ~[\r\n]* -> channel(HIDDEN);
WS: [ \n\r\t] -> channel(HIDDEN);

UNMATCHED: . ; //Should make an error