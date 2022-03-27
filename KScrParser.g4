parser grammar KScrParser;

options { tokenVocab = KScrLexer; }

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
constructorDecl: annotation* modifiers type parameters (COLON (THIS | id) LPAREN expr* RPAREN)? (block | SEMICOLON);
methodDecl: annotation* modifiers genericTypeDefs? type idPart parameters (block | SEMICOLON);

propGetter: 'get' block;
propSetter: 'set' block;
propertyDecl: annotation* modifiers type idPart ((ASSIGN expr SEMICOLON) | (block) | (LBRACE propGetter propSetter? RBRACE))?;
parameter: FINAL? type (indexer | ELIPSES)? idPart (ASSIGN expr)?;
parameters: LPAREN (parameter (COMMA parameter)*)? RPAREN;

block: (LBRACE statement* RBRACE | REQARROW expr SEMICOLON | statement) | SEMICOLON;

lightStatement
    : varDeclaration
    | varAssignment
    | varIncDecOp
    | ()
    ;
statement
    : innerCtorCall SEMICOLON
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
    : expr INSTANCEOF type                                        #instanceCheckValue
    | arr=expr LSQUAR index=expr RSQUAR                          #arrayIndexValue
    //| (expr | SUPER | THIS) DOT call                                #functionValue
    //| (expr | SUPER | THIS) DOT idPart                             #varValue
    | prefixop expr                                                 #prefixOpValue
    | typ=type left=idPart ASSIGN right=expr                        #inlineDeclareValue // can't use varDeclaration due to recursive rules
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

initialisation: NEW type arguments;
cast: LPAREN type RPAREN expr;
varDeclaration: type expr binaryop? ASSIGN expr;
varAssignment: expr binaryop? ASSIGN expr;
varIncDecOp: expr (PLUSPLUS | MINUSMINUS) | (PLUSPLUS | MINUSMINUS) expr;
call: idPart arguments;
innerCtorCall: (THIS | SUPER) arguments;
newArray: NEW type LSQUAR expr RSQUAR;
newListedArray: NEW type indexer LBRACE (expr (COMMA expr)*)? RBRACE;
indexer: LSQUAR RSQUAR;

arguments: LPAREN (expr (COMMA expr)*) RPAREN;

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