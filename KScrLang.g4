grammar KScrLang;

file: packageDecl? imports classDecl EOF;

packageDecl: PACKAGE id SEMICOLON;

imports: importDecl*;
importDecl: IMPORT STATIC? id (DOT STAR)? SEMICOLON;

classDecl: annotation* modifiers objectType genericTypeDefs? objectExtends? objectImplements? (LBRACE member* RBRACE | SEMICOLON);

objectExtends: EXTENDS type (COMMA type)*;
objectImplements: IMPLEMENTS type (COMMA type)*;

genericTypeDefs: LESSER genericTypeDef (COMMA genericTypeDef)* GREATER;
genericTypeDef: (CLASS? | ID) type (EXTENDS type*)? (SUPER type*)?;

objectType
    : CLASS
    | INTERFACE
    | ENUM
    | AT INTERFACE
    ;

member
    : function
    | constructor
    | propertyDecl SEMICOLON
    | classDecl
    | init
    | SEMICOLON
    ;

constructor: annotation* modifiers type LPAREN parameters RPAREN (COLON (THIS | id) LPAREN expr* RPAREN)? (block | SEMICOLON);
init: STATIC? block;

function: annotation* modifiers genericTypeDefs? type idPart LPAREN parameters RPAREN (functionBlock | functionArrow);

functionBlock: (block | SEMICOLON);
functionArrow: RDASHARROW (expr SEMICOLON | statement);

propGetter: 'get' block;
propSetter: 'set' block;
propertyDecl: annotation* modifiers type idPart ((ASSIGN expr SEMICOLON) | (block) | (LPAREN propGetter propSetter? RPAREN))?;
parameter: FINAL? type (indexer | ELIPSES)? idPart (ASSIGN expr)?;
parameters: (parameter (COMMA parameter)*)?;

block: LBRACE statement* RBRACE | RDASHARROW statement SEMICOLON;

statement
    : block
    | returnStatement SEMICOLON
    | throwStatement SEMICOLON
    | propertyDecl SEMICOLON
    | varAssignment SEMICOLON
    | initialisation SEMICOLON
    | varIncrement SEMICOLON
    | ifStatement
    | whileStatement
    | forStatement
    | fornStatement
    | foreachStatement
    | switchStatement
    | doWhile
    | yieldStatement
    | ((expr | SUPER | THIS) DOT)? call SEMICOLON
    | ctorCall SEMICOLON
    | SEMICOLON
    ;

annotation: AT id (LPAREN (annotationArg (COMMA annotationArg)* | expr)? RPAREN)?;
annotationArg: idPart ASSIGN expr;

type
    : annotation* rawType (genericTypeUses)?
    // e.g. @NonNull Integer @Nullable [] for a nullable array of nonnull integers
    | type annotation* indexer
    ;

genericTypeUses: LESSER genericTypeUse (COMMA genericTypeUse)* GREATER;
genericTypeUse: (QUESTION (EXTENDS | SUPER))? type;

rawType
    : primitiveType
    | inferType
    | id
    ;

primitiveType
    : BYTE
    | SHORT
    | INT
    | LONG
    | FLOAT
    | DOUBLE
    | VOID
    ;

inferType
    : VOID
    ;

modifiers: modifier*;

expr
    : expr DOT call                            #functionValue // can't merge due to recursive rules
    | expr EXCLAMATION? INSTANCEOF type        #instanceCheckValue
    | array=expr LSQUAR index=expr RSQUAR     #arrayIndexValue
    | expr DOT idPart                          #varValue
    | prefixop expr                            #prefixOpValue
    | expr postfixop                           #postfixOpValue
    | left=expr binaryop? ASSIGN right=expr   #inlineAssignValue // can't use varAssignment due to recursive rules
    | left=expr binaryop right=expr           #binaryOpValue
    | LPAREN expr RPAREN                       #parenValue
    | (SUPER DOT)? call                         #functionValue
    | initialisation                            #initialisationValue
    | switchStatement                           #switchValue
    | id DOT CLASS                              #classValue
    | primitiveType DOT CLASS                   #primitiveClassValue
    | cast                                      #castValue
    | newArray                                  #newArrayValue
    | newListedArray                            #newListedArrayValue
    | THIS                                      #thisValue
    | NUMLIT                                    #numLit
    | STRLIT                                    #strLit
    | BOOLLIT                                   #boolLit
    | RANGELIT                                  #rangeLit
    | NULL                                      #nullLit
    | idPart                                    #varValue
    ;

initialisation: NEW type LPAREN arguments RPAREN;
cast: LPAREN type RPAREN expr;
varAssignment: expr binaryop? ASSIGN expr;
call: idPart LPAREN arguments RPAREN;
ctorCall: (THIS | SUPER) LPAREN arguments RPAREN;
newArray: NEW type LSQUAR expr RSQUAR;
newListedArray: NEW type indexer LBRACE (expr (COMMA expr)*)? RBRACE;
indexer: LSQUAR RSQUAR;

arguments: (expr (COMMA expr)*)?;

returnStatement: RETURN expr?;
assertStatement: ASSERT expr (COLON STRLIT)?;
throwStatement: THROW expr;

ifStatement: IF LPAREN expr RPAREN block elseStatement?;
elseStatement: ELSE block;

whileStatement: WHILE LPAREN expr RPAREN block;
forStatement: FOR LPAREN start=statement? cond=expr SEMICOLON end=statement? RPAREN action=block;
fornStatement: FORN LPAREN start=id COLON range=RANGELIT RPAREN block;
foreachStatement: FOREACH LPAREN FINAL? type idPart COLON expr RPAREN block;
doWhile: DO block WHILE LPAREN expr RPAREN SEMICOLON;

switchStatement: SWITCH LPAREN expr RPAREN LBRACE caseClause* defaultClause? RBRACE;
caseClause: CASE expr (COLON statement | RDASHARROW expr SEMICOLON);
defaultClause: DEFAULT (COLON statement | RDASHARROW expr SEMICOLON);
yieldStatement: YIELD expr SEMICOLON;

varIncrement
    : expr (PLUSPLUS | MINUSMINUS)
    ;

binaryop
    : SLASH
    | STAR
    | PLUS
    | MINUS
    | PERCENT
    | BITAND
    | BITOR
    | AND
    | OR
    | UP
    | EQUAL
    | INEQUAL
    | GREATEREQ
    | LESSEREQ
    | GREATER
    | LESSER
    | LSHIFT
    | RSHIFT
    | ULSHIFT
    | URSHIFT
    ;

prefixop
    : PLUS
    | MINUS
    | EXCLAMATION
    | PLUSPLUS
    | MINUSMINUS
    ;

postfixop
    : PLUSPLUS
    | MINUSMINUS
    | UP DIGIT+
    ;

id: idPart (DOT idPart)*;
// contextual keywords are valid identifiers
idPart
    : ID
    | ANNOTATION
    | SEALED
    | PERMITS
    ;

modifier
    : PUBLIC
    | PROTECTED
    | PRIVATE
    | STATIC
    | FINAL
    | ABSTRACT
 //   | SYNCHRONISED
    | NATIVE
    ;

PROTECTED: 'protected';
PRIVATE: 'private';
PUBLIC: 'public';

SYNCHRONISED: 'synchronized';
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
FORN: 'forn';
FOR: 'for';
DO: 'do';
IF: 'if';

NUMLIT: MINUS? DIGIT+ ((DOT DIGIT+ ('f'? | 'd'))? | ('l' | 'i' | 's' | 'b')?);
STRLIT: QUOTE (ESCAPE_QUOTE | (~[\r\n"]))*? (QUOTE);
BOOLLIT: TRUE | FALSE;
RANGELIT: DIGIT+? TILDE DIGIT+?;
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

BOOL: 'boolean';
BYTE: 'byte';
SHORT: 'short';
CHAR: 'char';
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