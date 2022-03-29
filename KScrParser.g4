parser grammar KScrParser;

options { tokenVocab = KScrLexer; }

// file-related
packageDecl: PACKAGE id SEMICOLON;

imports: importDecl*;
importDecl: IMPORT STATIC? id (DOT STAR)? SEMICOLON;

// class-related
annotationArg: idPart ASSIGN expr;
annotation: AT id (LPAREN (annotationArg (COMMA annotationArg)* | expr)? RPAREN)?;

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
modifiers: modifier*;

classType
    : CLASS
    | INTERFACE
    | ENUM
    | AT INTERFACE
    ;

genericTypeUse: (QUESTION (EXTENDS | SUPER))? type;
genericTypeUses: LESSER genericTypeUse (COMMA genericTypeUse)* GREATER;

type
    : rawType genericTypeUses?
    | idPart
    | type genericTypeUses? (LSQUAR RSQUAR)
    | genericTypeUses? (LSQUAR RSQUAR)?
    ;

genericTypeDef: ID type (EXTENDS type | SUPER type)?;
genericTypeDefs: LESSER (NUMLIT | genericTypeDef) (COMMA genericTypeDef ELIPSES?)* GREATER;

objectExtends: EXTENDS type (COMMA type)*;
objectImplements: IMPLEMENTS type (COMMA type)*;

parameter: FINAL? type (indexer | ELIPSES)? idPart (ASSIGN expr)?;
parameters: LPAREN (parameter (COMMA parameter)*)? RPAREN;
arguments: LPAREN (expr (COMMA expr)*) RPAREN;

block: (LBRACE statement* RBRACE | REQARROW expr SEMICOLON | statement) | SEMICOLON;

initDecl: STATIC block;
subConstructorCall: type arguments;
subConstructorCalls: COLON subConstructorCall (COMMA subConstructorCall)*?;
constructorDecl: annotation* modifiers type parameters subConstructorCalls? (block | SEMICOLON);
methodDecl: annotation* modifiers genericTypeDefs? type idPart parameters (block | SEMICOLON);

propGetter: GET block;
propSetter: SET block;
propertyDecl: annotation* modifiers type idPart ((ASSIGN expr SEMICOLON) | (block) | (LBRACE propGetter propSetter? RBRACE) | SEMICOLON);

member
    : initDecl
    | propertyDecl
    | constructorDecl
    | methodDecl
    | classDecl
    ;

classDecl: annotation* modifiers classType idPart genericTypeDefs? objectExtends? objectImplements? (LBRACE member* RBRACE | SEMICOLON);

file: packageDecl imports classDecl* EOF;

rawType
    : primitiveLit
    | inferType
    | id
    ;

inferType: VOID | VAR;

indexer: LSQUAR RSQUAR;
cast: LPAREN type COLON expr RPAREN;
declaration: type assignment;
assignment: idPart binaryop? ASSIGN expr;
call: idPart arguments;
ctorCall: NEW type arguments;
newArray: NEW type LSQUAR NUMLIT RSQUAR;
newListedArray: NEW type indexer LBRACE (expr (COMMA expr)*)? RBRACE;

statement
    : declaration SEMICOLON
    | assignment SEMICOLON
    | returnStatement SEMICOLON
    | throwStatement SEMICOLON
    | ctorCall SEMICOLON
    | pipeStatement SEMICOLON
    | tryCatchStatement finallyBlock?
    | tryWithResourcesStatement finallyBlock?
    | markStatement | jumpStatement
    | ifStatement finallyBlock?
    | whileStatement finallyBlock?
    | doWhile finallyBlock?
    | forStatement finallyBlock?
    | foreachStatement finallyBlock?
    | switchStatement finallyBlock?
    | SEMICOLON
    ;
expr
    : expr INSTANCEOF type                                    #checkInstanceof
    | arr=expr LSQUAR index=expr RSQUAR                       #readArray
    | declaration                                             #varDeclare // can't use varDeclaration due to recursive rules
    | assignment                                              #varAssign // can't use varAssignment due to recursive rules
    | prefixop expr                                           #opPrefix
    | left=expr binaryop right=expr                           #opBinary
    | expr postfixop                                          #opPostfix
    | LPAREN expr RPAREN                                      #parens
    | call                                                    #callMember
    | ctorCall                                                #callCtor
    | expr DOT idPart                                         #callProperty
    | expr DOT call                                           #callMethod
    | nullable=expr QUESTION QUESTION fallback=expr           #exprNullFallback
    | throwStatement                                          #exprThrow
    | switchStatement                                         #exprSwitch
    | cast                                                    #exprCast
    | newArray                                                #newArrayValue
    | newListedArray                                          #newListedArrayValue
    | primitiveLit                                            #thisValue
    | idPart                                                  #varValue
    ;

returnStatement: RETURN expr?;
throwStatement: THROW expr;

markStatement: MARK idPart SEMICOLON;
jumpStatement: JUMP idPart SEMICOLON;

tryCatchStatement: TRY block CATCH block;
tryWithResourcesStatement: TRY LPAREN declaration (COMMA declaration)* RPAREN block CATCH block;
finallyBlock: FINALLY block;

ifStatement: IF LPAREN expr RPAREN block elseStatement?;
elseStatement: ELSE block;

whileStatement: WHILE LPAREN expr RPAREN block;
forStatement: FOR LPAREN start=statement? SEMICOLON cond=expr? SEMICOLON end=statement? RPAREN action=block;
foreachStatement: FOREACH LPAREN idPart COLON expr RPAREN block;
doWhile: DO block WHILE LPAREN expr RPAREN SEMICOLON;

pipeStatement: pipeWriteStatement*? expr pipeReadStatement*?;
pipeWriteStatement: expr (RBOXARROW | URSHIFT | RSHIFT);
pipeReadStatement: (LSHIFT | ULSHIFT | LBOXARROW) expr;

switchStatement: SWITCH LPAREN expr RPAREN LBRACE caseClause* defaultClause? RBRACE;
caseClause: CASE expr block;
defaultClause: DEFAULT block;

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
    | LBOXARROW
    | RBOXARROW
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
    | SUPER
    | TYPE
    | ENUM
    | NUMLIT
    | BOOLLIT
    | STRLIT
    | STDIOLIT
    | NULL
    ;