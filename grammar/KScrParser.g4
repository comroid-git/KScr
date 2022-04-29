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
    | ANNOTATION    #ctAnnotation
    ;

genericTypeUses: LESSER (n=NUMLIT | first=type) (COMMA type)* GREATER;

type
    : idPart                                            #importedTypeName
    | rawType genericTypeUses?                          #normalTypeUse
    | rawType genericTypeUses? (indexer | ELIPSES)?     #arrayTypeUse
    ;

rawType
    : primitiveTypeLit
    | id
    ;

genericTypeDef: idPart elp=ELIPSES? (EXTENDS ext=type | SUPER sup=type)? (ASSIGN (defN=NUMLIT | def=type))?;
genericTypeDefs: LESSER (NUMLIT | genericTypeDef) (COMMA genericTypeDef)* GREATER;

objectExtends: EXTENDS type (COMMA type)*;
objectImplements: IMPLEMENTS type (COMMA type)*;

parameter: FINAL? type idPart (ASSIGN expr)?;
parameters: LPAREN (parameter (COMMA parameter)*)? RPAREN;
arguments: LPAREN (expr (COMMA expr)*)? RPAREN;

noBlock: SEMICOLON;
normalBlock: LBRACE statement* RBRACE;
memberBlock
    : normalBlock               #memberNormalBlock
    | REQARROW expr SEMICOLON   #memberExprBlock
    | noBlock                   #memberNoBlock
    ;
codeBlock
    : normalBlock               #codeNormalBlock
    | statement                 #codeStmtBlock
    | noBlock                   #codeNoBlock
    ;

initDecl: STATIC memberBlock;
subConstructorCall: type arguments;
subConstructorCalls: COLON subConstructorCall (COMMA subConstructorCall)*?;
constructorDecl: annotation* modifiers type parameters subConstructorCalls? memberBlock;
methodDecl: annotation* modifiers genericTypeDefs? type idPart parameters memberBlock;

propGetter: GET memberBlock;
propSetter: SET memberBlock;
propInit: INIT memberBlock;
propBlock
    : memberBlock                                       #propComputed
    | LBRACE propGetter propSetter? propInit? RBRACE    #propAccessors
    | (ASSIGN expr)? SEMICOLON                          #propFieldStyle
    ;
propertyDecl: annotation* modifiers type idPart propBlock;

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
declaration: type idPart ASSIGN expr;
mutation: binaryop? ASSIGN expr;
call: idPart arguments;
ctorCall: NEW type arguments;
newArray: NEW type LSQUAR NUMLIT RSQUAR;
newListedArray: NEW type indexer LBRACE (expr (COMMA expr)*)? RBRACE;

statement
    : declaration SEMICOLON                                 #stmtDeclare
    | left=expr mutation SEMICOLON                          #stmtAssign
    | left=expr DOT idPart arguments?                       #stmtCallMember
    | returnStatement SEMICOLON                             #stmtReturn
    | throwStatement SEMICOLON                              #stmtThrow
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
    | expr (pipeRead | pipeWrite)+ SEMICOLON                #stmtPipe
    | SEMICOLON                                             #stmtEmpty
    ;
expr
    : idPart                                                #varValue
    | expr INSTANCEOF type                                  #checkInstanceof
    | YIELD expr                                            #yieldExpr
    | arr=expr LSQUAR index=expr RSQUAR                     #readArray
    | declaration                                           #varDeclare // can't use varDeclaration due to recursive rules
    | left=expr mutation                                    #varAssign // can't use varAssignment due to recursive rules
    | left=expr DOT idPart arguments?                       #exprCallMember
    | LPAREN expr RPAREN                                    #parens
    | ctorCall                                              #callCtor
    | nullable=expr QUESTION QUESTION fallback=expr         #exprNullFallback
    | throwStatement                                        #exprThrow
    | switchStatement                                       #exprSwitch
    | cast                                                  #exprCast
    | newArray                                              #newArrayValue
    | newListedArray                                        #newListedArrayValue
    | primitiveLit                                          #nativeLitValue
    | type                                                  #typeValue
    | left=expr TILDE right=expr                            #rangeInvoc
    | prefixop expr                                         #opPrefix
    | left=expr binaryop right=expr                         #opBinary
    | expr postfixop                                        #opPostfix
//    | expr (pipeRead | pipeWrite)+                          #exprPipe
    ;

returnStatement: YIELD? RETURN expr?;
throwStatement: THROW expr;

markStatement: MARK idPart SEMICOLON;
jumpStatement: JUMP idPart SEMICOLON;

tryCatchStatement: TRY codeBlock CATCH codeBlock;
tryWithResourcesStatement: TRY LPAREN declaration (COMMA declaration)* RPAREN codeBlock CATCH codeBlock;
finallyBlock: FINALLY codeBlock;

ifStatement: IF LPAREN expr RPAREN codeBlock elseStatement?;
elseStatement: ELSE codeBlock;

whileStatement: WHILE LPAREN expr RPAREN codeBlock;
forStatement: FOR LPAREN init=statement cond=expr SEMICOLON acc=expr RPAREN codeBlock;
foreachStatement: FOREACH LPAREN idPart COLON expr RPAREN codeBlock;
doWhile: DO codeBlock WHILE LPAREN expr RPAREN SEMICOLON;

pipeRead: rPipeOp expr;
pipeWrite: lPipeOp expr;
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
    | EXCLAMATION   #opBitNot
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
primitiveTypeLit 
    : OBJECT        #typeLitObject
    | array         #typeLitArray
    | tuple         #typeLitTuple
    | num           #typeLitNum
    | TYPE          #typeLitType
    | ENUM          #typeLitEnum
    | VOID          #typeLitVoid
    ;
primitiveLit
    : THIS          #varThis
    | SUPER         #varSuper
    | NUMLIT        #varLitNum
    | TRUE          #varLitTrue
    | FALSE         #varLitFalse
    | STRLIT        #varLitStr
    | STDIOLIT      #varLitStdio
    | ENDLLIT       #varLitEndl
    | NULL          #varLitNull
    ;