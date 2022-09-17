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
    | rawType genericTypeUses? (indexerEmpty | ELIPSES)?     #arrayTypeUse
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

statements: statement*;
noBlock: SEMICOLON;
uniformBlock: expr | statement;
normalBlock: LBRACE statements RBRACE;
memberExpr: REQARROW uniformBlock;
lambdaBlock: RDASHARROW (uniformBlock | normalBlock);
caseBlock
    : COLON statements BREAK( SEMICOLON)?   #caseStmtBlock
    | memberExpr COMMA                      #caseExprBlock
    ;
memberBlock
    : normalBlock               #memberNormalBlock
    | memberExpr SEMICOLON      #memberExprBlock
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
indexerMemberDecl: annotation* modifiers genericTypeDefs? type THIS indexerDecl propBlock;

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
    : propertyDecl                  #memProp
    | classDecl                     #memCls
    | initDecl catchBlocks?         #memInit
    | constructorDecl catchBlocks?  #memCtor
    | methodDecl catchBlocks?       #memMtd
    | indexerMemberDecl             #memIdx
    ;

classDecl: annotation* modifiers classType idPart genericTypeDefs? objectExtends? objectImplements? (LBRACE member* RBRACE | SEMICOLON);

file: packageDecl imports classDecl* EOF;

inferType: VOID | VAR;

indexerEmpty: LSQUAR RSQUAR;
indexerDecl: LSQUAR type idPart (COMMA type idPart)* RSQUAR;
indexerExpr: LSQUAR expr (COMMA expr)* RSQUAR;
cast: LPAREN type COLON expr RPAREN;
declaration: type idPart (ASSIGN expr)?;
mutation: binaryop? ASSIGN expr;
call: idPart arguments;
ctorCall: NEW type arguments;
newArray: NEW type indexerExpr;
newListedArray: NEW type indexerEmpty LBRACE (expr (COMMA expr)*)? RBRACE;
label: idPart COLON WS;
lambda
    : label? type COLON idPart        #methodRef
    | label? tupleExpr lambdaBlock    #lambdaExpr 
    ;

returnStatement: YIELD? RETURN expr?;
throwStatement: THROW expr;

markStatement: MARK idPart SEMICOLON;
jumpStatement: JUMP idPart SEMICOLON;

tryCatchStatement: TRY codeBlock;
tryWithResourcesStatement: TRY LPAREN declaration (COMMA declaration)* RPAREN codeBlock;
catchBlocks: catchBlock* finallyBlock;
catchBlock: CATCH (LPAREN type (COMMA type)* idPart RPAREN)? codeBlock;
finallyBlock: FINALLY codeBlock;

ifStatement: IF LPAREN expr RPAREN codeBlock elseStatement?;
elseStatement: ELSE codeBlock;

whileStatement: WHILE LPAREN expr RPAREN codeBlock;
forStatement: FOR LPAREN init=statement cond=expr SEMICOLON acc=expr RPAREN codeBlock;
foreachStatement: FOREACH LPAREN idPart COLON expr RPAREN codeBlock;
doWhile: DO codeBlock WHILE LPAREN expr RPAREN SEMICOLON;

switchStatement: SWITCH tupleExpr LBRACE caseClause* RBRACE;
caseClause: ((CASE tupleExpr) | (DEFAULT)) caseBlock;

statement
    : declaration SEMICOLON                                 #stmtDeclare
    | left=expr mutation SEMICOLON                          #stmtAssign
    | left=tupleExpr ASSIGN right=tupleExpr                 #stmtAssignTuple
    | left=expr DOT idPart arguments?                       #stmtCallMember
    | returnStatement SEMICOLON                             #stmtReturn
    | throwStatement SEMICOLON                              #stmtThrow
    | markStatement                                         #stmtMark
    | jumpStatement                                         #stmtJump
    | tryCatchStatement catchBlocks?                        #stmtTryCatch
    | tryWithResourcesStatement catchBlocks?                #stmtTryWithRes
    | ifStatement catchBlocks?                              #stmtIf
    | whileStatement catchBlocks?                           #stmtWhile
    | doWhile catchBlocks?                                  #stmtDoWhile
    | forStatement catchBlocks?                             #stmtFor
    | foreachStatement catchBlocks?                         #stmtForeach
    | switchStatement catchBlocks?                          #stmtSwitch
    | pipe=expr (RREQARROW lambda)+ SEMICOLON               #stmtPipeListen
    | pipe=expr (RRDASHARROW expr)+ SEMICOLON               #stmtPipeRead
    | pipe=expr (LLDASHARROW expr)+ SEMICOLON               #stmtPipeWrite
    | SEMICOLON                                             #stmtEmpty
    ;
typedExpr: type? expr;
expr
    // simply a variable
    : idPart                                                #varValue
    // `is` keyword
    | expr IS type idPart?                                  #checkInstanceof
    // syntax components
    | YIELD expr                                            #yieldExpr
    | target=expr indexerExpr                                #readIndexer
    | LPAREN expr RPAREN                                    #parens
    | cast                                                  #exprCast
    | newArray                                              #newArrayValue
    | newListedArray                                        #newListedArrayValue
    | primitiveLit                                          #nativeLitValue
    | type                                                  #typeValue
    | lambda                                                #exprLambda
    // variable mutation
    | declaration                                           #varDeclare // can't use varDeclaration due to recursive rules
    | left=expr mutation                                    #varAssign // can't use varAssignment due to recursive rules
    // member calls
    | left=expr DOT idPart arguments?                       #exprCallMember
    | ctorCall                                              #callCtor
    // statement expressions
    | throwStatement                                        #exprThrow
    | switchStatement                                       #exprSwitch
    // range invocator
    | left=expr SHORTELIPSES right=expr                     #rangeInvoc
    // pipe operators
    | pipe=expr (RREQARROW lambda)+                         #exprPipeListen
    // operators
    | prefixop expr                                         #opPrefix
    | left=expr binaryop right=expr                         #opBinary
    | expr postfixop                                        #opPostfix
    // tuple expressions
    | tupleExpr                                             #exprTuple
    | UNDERSCORE                                            #exprVoid
    ;

tupleDeclType: type idPart?;
tupleDecl: LPAREN tupleDeclType (COMMA tupleDeclType)* RPAREN;
tupleExpr: LPAREN typedExpr (COMMA typedExpr)* RPAREN;

binaryop
    : PLUS                  #opPlus
    | MINUS                 #opMinus
    | STAR                  #opMultiply
    | SLASH                 #opDivide
    | PERCENT               #opModulus
    | BITAND                #opBitAnd
    | BITOR                 #opBitOr
    | EXCLAMATION           #opBitNot
    | AND                   #opLogicAnd
    | OR                    #opLogicOr
    | UP                    #opPow
    | EQUAL                 #opEqual
    | INEQUAL               #opInequal
    | GREATEREQ             #opGreaterEq
    | LESSEREQ              #opLesserEq
    | GREATER               #opGreater
    | LESSER                #opLesser
    | LSHIFT                #opLShift
    | RSHIFT                #opRShift
    | ULSHIFT               #opULShift
    | URSHIFT               #opURShift
    | QUESTION QUESTION     #opNullFallback
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