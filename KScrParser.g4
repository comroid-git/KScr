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
    : PUBLIC        #modPublic
    | INTERNAL      #modInternal
    | PROTECTED     #modProtected
    | PRIVATE       #modPrivate
    | STATIC        #modStatic
    | FINAL         #modFinal
    | ABSTRACT      #modAbstract
    | SYNCHRONIZED  #modSyncronized
    | NATIVE        #modNative
    ;
modifiers: modifier*;

classType
    : CLASS         #ctClass
    | INTERFACE     #ctInterface
    | ENUM          #ctEnum
    | AT INTERFACE  #ctAnnotation
    ;

genericTypeUses: LESSER (n=NUMLIT | first=type) (COMMA type)* GREATER;

type
    : idPart                                            #importedTypeName
    | rawType genericTypeUses?                          #normalTypeUse
    | rawType genericTypeUses? (LSQUAR RSQUAR)          #arrayTypeUse
    ;

rawType
    : primitiveLit
    //| inferType
    | id
    ;

genericTypeDef: idPart elp=ELIPSES? (EXTENDS ext=type | SUPER sup=type)? (ASSIGN (defN=NUMLIT | def=type))?;
genericTypeDefs: LESSER (NUMLIT | genericTypeDef) (COMMA genericTypeDef)* GREATER;

objectExtends: EXTENDS type (COMMA type)*;
objectImplements: IMPLEMENTS type (COMMA type)*;

parameter: FINAL? type (indexer | ELIPSES)? idPart (ASSIGN expr)?;
parameters: LPAREN (parameter (COMMA parameter)*)? RPAREN;
arguments: LPAREN (expr (COMMA expr)*) RPAREN;

memberBlock
    : LBRACE statement* RBRACE  #memberBodyBlock
    | REQARROW expr SEMICOLON   #memberExprBlock
    | SEMICOLON                 #memberNoBlock
    ;
codeBlock
    : LBRACE statement* RBRACE  #codeBodyBlock
    | statement                 #codeStmtBlock
    | SEMICOLON                 #codeNoBlock
    ;

initDecl: STATIC codeBlock;
subConstructorCall: type arguments;
subConstructorCalls: COLON subConstructorCall (COMMA subConstructorCall)*?;
constructorDecl: annotation* modifiers type parameters subConstructorCalls? codeBlock;
methodDecl: annotation* modifiers genericTypeDefs? type idPart parameters codeBlock;

propGetter: GET memberBlock;
propSetter: SET memberBlock;
propertyDecl: annotation* modifiers type idPart ((ASSIGN expr SEMICOLON) | memberBlock | (LBRACE propGetter propSetter? RBRACE) | SEMICOLON);

member
    : initDecl
    | propertyDecl
    | constructorDecl
    | methodDecl
    | classDecl
    ;

classDecl: annotation* modifiers classType idPart genericTypeDefs? objectExtends? objectImplements? (LBRACE member* RBRACE | SEMICOLON);

file: packageDecl imports classDecl* EOF;

inferType: VOID | VAR;

indexer: LSQUAR RSQUAR;
cast: LPAREN type COLON expr RPAREN;
declaration: type idPart binaryop? ASSIGN expr;
call: idPart arguments;
ctorCall: NEW type arguments;
newArray: NEW type LSQUAR NUMLIT RSQUAR;
newListedArray: NEW type indexer LBRACE (expr (COMMA expr)*)? RBRACE;

statement
    : declaration SEMICOLON                                 #stmtDeclare
    | left=expr binaryop? ASSIGN right=expr SEMICOLON       #stmtAssign
    | returnStatement SEMICOLON                             #stmtReturn
    | throwStatement SEMICOLON                              #stmtThrow
    | ctorCall SEMICOLON                                    #stmtCtor
    | pipeStatement SEMICOLON                               #stmtPipe
    | tryCatchStatement finallyBlock?                       #stmtTryCatch
    | tryWithResourcesStatement finallyBlock?               #stmtTryWithRes
    | markStatement                                         #stmtMark
    | jumpStatement                                         #stmtJump
    | ifStatement finallyBlock?                             #stmtIf
    | whileStatement finallyBlock?                          #stmtWhile
    | doWhile finallyBlock?                                 #stmtDoWhile
    | forStatement finallyBlock?                            #stmtFor
    | foreachStatement finallyBlock?                        #stmtForeach
    | switchStatement finallyBlock?                         #stmtSwitch
    | SEMICOLON                                             #stmtEmpty
    ;
expr
    : expr INSTANCEOF type                                  #checkInstanceof
    | arr=expr LSQUAR index=expr RSQUAR                     #readArray
    //| declaration                                           #varDeclare // can't use varDeclaration due to recursive rules
    | left=expr binaryop? ASSIGN right=expr                 #varAssign // can't use varAssignment due to recursive rules
    | prefixop expr                                         #opPrefix
    | left=expr binaryop right=expr                         #opBinary
    | expr postfixop                                        #opPostfix
    | LPAREN expr RPAREN                                    #parens
    | ctorCall                                              #callCtor
    | expr DOT idPart arguments?                            #callMember
    | nullable=expr QUESTION QUESTION fallback=expr         #exprNullFallback
    | throwStatement                                        #exprThrow
    | switchStatement                                       #exprSwitch
    | cast                                                  #exprCast
    | newArray                                              #newArrayValue
    | newListedArray                                        #newListedArrayValue
    | primitiveLit                                          #thisValue
    | idPart                                                #varValue
    | left=expr TILDE right=expr                            #rangeInvoc
    ;

returnStatement: RETURN expr?;
throwStatement: THROW expr;

markStatement: MARK idPart SEMICOLON;
jumpStatement: JUMP idPart SEMICOLON;

tryCatchStatement: TRY codeBlock CATCH codeBlock;
tryWithResourcesStatement: TRY LPAREN declaration (COMMA declaration)* RPAREN codeBlock CATCH codeBlock;
finallyBlock: FINALLY codeBlock;

ifStatement: IF LPAREN expr RPAREN codeBlock elseStatement?;
elseStatement: ELSE codeBlock;

whileStatement: WHILE LPAREN expr RPAREN codeBlock;
forStatement: FOR LPAREN start=statement? SEMICOLON cond=expr? SEMICOLON end=statement? RPAREN action=codeBlock;
foreachStatement: FOREACH LPAREN idPart COLON expr RPAREN codeBlock;
doWhile: DO codeBlock WHILE LPAREN expr RPAREN SEMICOLON;

pipeStatement: pipeWriteStatement*? expr pipeReadStatement*?;
pipeWriteStatement: expr rPipeOp;
pipeReadStatement: lPipeOp expr;
lPipeOp
    : LLDASHARROW   #opPipeLLD
    | LLEQARROW     #opPipeLLE
    | LPULLARROW    #opPipeLPL
    | LBOXARROW     #opPipeLBX
    ;
rPipeOp
    : RRDASHARROW   #opPipeRRD
    | RREQARROW     #opPipeRRE
    | RPULLARROW    #opPipeRPL
    | RBOXARROW     #opPipeRBX
    ;

switchStatement: SWITCH LPAREN expr RPAREN LBRACE caseClause* defaultClause? RBRACE;
caseClause: CASE expr codeBlock;
defaultClause: DEFAULT codeBlock;

binaryop
    : PLUS          #opPlus
    | MINUS         #opMinus
    | STAR          #opMultiply
    | SLASH         #opDivide
    | PERCENT       #opModulus
    | BITAND        #opBitAnd
    | BITOR         #opBitOr
    | AND           #opLogicAnd
    | OR            #opLogicOr
    | UP            #opPow
    | EQUAL         #opEqual
    | INEQUAL       #opInequal
    | GREATEREQ     #opGreaterEq
    | LESSEREQ      #opLesserEq
    | GREATER       #opGreater
    | LESSER        #opLesser
    | LSHIFT        #opLShift
    | RSHIFT        #opRShift
    | ULSHIFT       #opULShift
    | URSHIFT       #opURShift
    ;

prefixop
    : MINUS         #opArithNot
    | EXCLAMATION   #opLogicNot
    | PLUSPLUS      #opIncrRead
    | MINUSMINUS    #opDecrRead
    ;

postfixop
    : PLUSPLUS      #opReadIncr
    | MINUSMINUS    #opReadDecr
    ;

id: idPart (DOT idPart)*;
// contextual keywords are valid identifiers
idPart
    : ID
    | ANNOTATION
    ;


array: ARRAYIDENT genericTypeUses?;
tuple: TUPLEIDENT genericTypeUses?;
num: (NUMIDENT | INT) genericTypeUses?  #numTypeLitTuple
    | BYTE                              #numTypeLitByte
    | SHORT                             #numTypeLitShort
    | INT                               #numTypeLitInt
    | LONG                              #numTypeLitLong
    | FLOAT                             #numTypeLitFloat
    | DOUBLE                            #numTypeLitDouble
    ;
primitiveLit:
      OBJECT        #typeLitObject
    | array         #typeLitArray
    | tuple         #typeLitTuple
    | num           #typeLitNum
    | TYPE          #typeLitType
    | ENUM          #typeLitEnum
    | THIS          #varThis
    | SUPER         #varSuper
    | NUMLIT        #varLitNum
    | TRUE          #varLitTrue
    | FALSE         #varLitFalse
    | STRLIT        #varLitStr
    | STDIOLIT      #varLitStdio
    | NULL          #varLitNull
    ;