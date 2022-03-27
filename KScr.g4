grammar KScr;

file: packageDecl imports classDecl* EOF;

packageDecl: PACKAGE id SEMICOLON;

imports: importDecl*;
importDecl: IMPORT STATIC? id (DOT STAR)? SEMICOLON;

classDecl: annotation* modifiers classType idPart genericTypeDefs? objectExtends? objectImplements? (LBRACE member* RBRACE | SEMICOLON);

objectExtends: EXTENDS type (COMMA type)*;
objectImplements: IMPLEMENTS type (COMMA type)*;

genericTypeDefs: LESSER (NUMLIT | genericTypeDef) (COMMA genericTypeDef ELIPSES?)* GREATER;
genericTypeDef: ID type (EXTENDS type | SUPER type)?;

classType
    : CLASS
    | INTERFACE
    | ENUM
    | AT INTERFACE
    ;

member
    : methodDecl
    | constructorDecl
    | propertyDecl
    | classDecl
    | initDecl
    | SEMICOLON
    ;

initDecl: STATIC block;
constructorDecl: annotation* modifiers type LPAREN parameters RPAREN (COLON (THIS | id) LPAREN expr* RPAREN)? (block | SEMICOLON);
methodDecl: annotation* modifiers genericTypeDefs? type idPart LPAREN parameters RPAREN (block | SEMICOLON);

propGetter: 'get' block;
propSetter: 'set' block;
propertyDecl: annotation* modifiers type idPart ((ASSIGN expr SEMICOLON) | (block) | (LBRACE propGetter propSetter? RBRACE))?;
parameter: FINAL? type (indexer | ELIPSES)? idPart (ASSIGN expr)?;
parameters: (parameter (COMMA parameter)*)?;

block: (LBRACE statement* RBRACE | REQARROW expr SEMICOLON | statement) | SEMICOLON;

lightStatement
    : varDeclaration
    | varAssignment
    | varIncDecOp
    | ()
    ;
statement
    : ctorCall SEMICOLON
    | lightStatement SEMICOLON
    | returnStatement SEMICOLON
    | throwStatement SEMICOLON
    | initialisation SEMICOLON
    | pipeStatement SEMICOLON
    | tryCatchStatement finallyBlock?
    | tryWithResourcesStatement finallyBlock?
    | ifStatement finallyBlock?
    | whileStatement finallyBlock?
    | doWhile finallyBlock?
    | forStatement finallyBlock?
    | foreachStatement finallyBlock?
    | switchStatement finallyBlock?
    | SEMICOLON
    ;

annotation: AT id (LPAREN (annotationArg (COMMA annotationArg)* | expr)? RPAREN)?;
annotationArg: idPart ASSIGN expr;

type
    : rawType genericTypeUses?
    | type genericTypeUses? (LSQUAR RSQUAR)
    | genericTypeUses? (LSQUAR RSQUAR)?
    ;

genericTypeUses: LESSER genericTypeUse (COMMA genericTypeUse)* GREATER;
genericTypeUse: (QUESTION (EXTENDS | SUPER))? type;

rawType
    : primitiveLit
    | inferType
    | id
    ;

inferType: VOID | VAR;

modifiers: modifier*;

expr
    : expr EXCLAMATION? INSTANCEOF type                             #instanceCheckValue
    | arr=expr LSQUAR index=expr RSQUAR                          #arrayIndexValue
    | (expr | SUPER | THIS) DOT call                                #functionValue
    | (expr | SUPER | THIS) DOT idPart                             #varValue
    | prefixop expr                                                 #prefixOpValue
    | typ=type left=expr ASSIGN right=expr                        #inlineDeclareValue // can't use varDeclaration due to recursive rules
    | left=expr binaryop? ASSIGN right=expr                        #inlineAssignValue // can't use varAssignment due to recursive rules
    | left=expr binaryop right=expr                                #binaryOpValue
    | expr postfixop                                                #postfixOpValue
    | LPAREN expr RPAREN                                            #parenValue
    | initialisation                                                 #initialisationValue
    | switchStatement                                                #switchValue
    | expr (LSHIFT | RSHIFT | LBOXARROW | RBOXARROW) expr           #pipeOperatorStatement // can't use pipeStatement due to recursive rules
    | id DOT call                                                   #classValue
    | primitiveLit DOT call                                        #primitiveClassValue
    | cast                                                           #castValue
    | newArray                                                       #newArrayValue
    | newListedArray                                                 #newListedArrayValue
    | primitiveLit                                                 #thisValue
    | idPart                                                         #varValue
    ;

initialisation: NEW type LPAREN arguments RPAREN;
cast: LPAREN type RPAREN expr;
varDeclaration: type expr binaryop? ASSIGN expr;
varAssignment: expr binaryop? ASSIGN expr;
call: idPart LPAREN arguments RPAREN;
ctorCall: (THIS | SUPER) LPAREN arguments RPAREN;
newArray: NEW type LSQUAR expr RSQUAR;
newListedArray: NEW type indexer LBRACE (expr (COMMA expr)*)? RBRACE;
indexer: LSQUAR RSQUAR;

arguments: (expr (COMMA expr)*)?;

returnStatement: RETURN expr?;
throwStatement: THROW expr;

pipeStatement: expr (LSHIFT | RSHIFT | LBOXARROW | RBOXARROW) expr;

tryCatchStatement: TRY block CATCH block;
tryWithResourcesStatement: TRY LPAREN varDeclaration (COMMA varDeclaration)* RPAREN block CATCH block;
finallyBlock: FINALLY block;

ifStatement: IF LPAREN expr RPAREN block elseStatement?;
elseStatement: ELSE block;

whileStatement: WHILE LPAREN expr RPAREN block;
forStatement: FOR LPAREN start=lightStatement? SEMICOLON cond=lightStatement SEMICOLON end=lightStatement? RPAREN action=block;
foreachStatement: FOREACH LPAREN idPart COLON expr RPAREN block;
doWhile: DO block WHILE LPAREN expr RPAREN SEMICOLON;

switchStatement: SWITCH LPAREN expr RPAREN LBRACE caseClause* defaultClause? RBRACE;
caseClause: CASE expr (COLON statement | RDASHARROW expr SEMICOLON);
defaultClause: DEFAULT (COLON statement | RDASHARROW expr SEMICOLON);

varIncDecOp
    : expr (PLUSPLUS | MINUSMINUS)
    | (PLUSPLUS | MINUSMINUS) expr
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
    | TILDE
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
    | INTERNAL
    | PROTECTED
    | PRIVATE
    | STATIC
    | FINAL
    | ABSTRACT
    | SYNCHRONIZED
    | NATIVE
    ;

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

array: ARRAYIDENT genericTypeUses?;
tuple: TUPLEIDENT genericTypeUses?;
num: (NUMIDENT | INT) genericTypeUses? 
    | BYTE
    | SHORT
    | INT
    | LONG
    | FLOAT
    | DOUBLE
    ;
primitiveLit:
      OBJECT 
    | array 
    | tuple
    | num
    | THIS
    | TYPE
    | ENUM
    | NUMLIT
    | BOOLLIT
    | STRLIT
    | STDIOLIT
    | NULL
    ;

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