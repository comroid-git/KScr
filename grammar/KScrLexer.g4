lexer grammar KScrLexer;

// file modifiers
PACKAGE: 'package';
IMPORT: 'import';

// accessibility modifier
PUBLIC: 'public';
INTERNAL: 'internal';
PROTECTED: 'protected';
PRIVATE: 'private';

// common modifiers
STATIC: 'static';
FINAL: 'final';
ABSTRACT: 'abstract';
NATIVE: 'native';
SYNCHRONIZED: 'synchronized';

// class types
CLASS: 'class';
INTERFACE: 'interface';
ENUM: 'enum';
ANNOTATION: 'annotation';

// class footprint modifiers
EXTENDS: 'extends';
IMPLEMENTS: 'implements';

// common statements
RETURN: 'return';
THROW: 'throw';
NEW: 'new';
YIELD: 'yield';
IS: 'is';

// complex statements
MARK: 'mark';
JUMP: 'jump';
IF: 'if';
ELSE: 'else';
FOR: 'for';
FOREACH: 'foreach';
DO: 'do';
WHILE: 'while';
SWITCH: 'switch';
CASE: 'case';
DEFAULT: 'default';
BREAK: 'break';
CONTINUE: 'continue';
TRY: 'try';
CATCH: 'catch';
FINALLY: 'finally';

// primitive types
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

// literals
NUMLIT
    : DIGIT+ (((DOT DIGIT+)? ('f'? | 'd'))? | ('l' | 'i' | 's' | 'b')?)
    | '0' ('x' | 'b') (DIGIT | [AaBbCcDdEeFf])+
    ;
STRLIT: QUOTE (ESCAPE_QUOTE | (~[\r\n"]))*? (QUOTE);
STDIOLIT: 'stdio';
ENDLLIT: 'endl';
NULL: 'null';
TRUE: 'true';
FALSE: 'false';

// common expressions
THIS: 'this';
SUPER: 'super';
GET: 'get';
SET: 'set';
INIT: 'init';

// logistic symbols
LPAREN: '(';
RPAREN: ')';
LBRACE: '{';
RBRACE: '}';
LSQUAR: '[';
RSQUAR: ']';

COLON: ':';
SEMICOLON: ';';
DOT: '.';
COMMA: ',';
EXCLAMATION: '!';
QUESTION: '?';
SHORTELIPSES: '..';
ELIPSES: '...';
TILDE: '~';

ESCAPE_QUOTE: '\\"';
QUOTE: '"';

// operators
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

REQARROW: '=>';
LDASHARROW: '<-';
RDASHARROW: '->';
LLDASHARROW: '<<-';
RRDASHARROW: '->>';
//LLEQARROW: '<<=';
RREQARROW: '=>>';
//LPULLARROW: '=<<';
//RPULLARROW: '>>=';
//LBOXARROW: '<|';
//RBOXARROW: '|>';
//DBOXARROW: '<|>';

fragment ID: LETTER (DIGIT | LETTER)*;
fragment DIGIT: [0-9];
fragment LETTER: [a-zA-Z_$£#];

SING_COMMENT: '//' ~[\r\n]* -> channel(HIDDEN);
WS: [ \n\r\t] -> channel(HIDDEN);

UNMATCHED: . ; //Should make an error