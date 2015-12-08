\ autohipnosis.fs

\ This is the main file of «Autohipnosis»,
\ an experimental interactive fiction in Spanish.

\ Version 0.0.0-20151208.

\ http://programandala.net/es.programa.autohipnosis

\ Copyright (C) 2012,2015 Marcos Cruz (programandala.net)

\ You may do whatever you want with this work, so long as you
\ retain the copyright notice(s) and this license in all
\ redistributed copies and derived works. There is no warranty.

\ }}} ##########################################################

\ Este programa está escrito en Forth usando el sistema Gforth:
\ <http://www.jwdt.com/~paysan/gforth.html>
\ Para escribir este programa se ha empleado el editor Vim:
\ <http://www.vim.org>

\ Historial de desarrollo:
\ <http://programandala.net/es.programa.autohipnosis.historial.html>

\ Información sobre juegos conversacionales:
\ <http://caad.es>
 
\ }}} ##########################################################
\ Notación de la pila {{{

0 [if]

En este programa usamos las siguientes abreviaturas para
describir los elementos de la pila:

+n    = número de 32 bitios positivo
-n    = número de 32 bitios negativo
...   = elipsis: número variable de elementos, o rango
a     = dirección de memoria
a u   = dirección y longitud de zona de memoria, p.e. de un texto
b     = octeto, valor de ocho bitios
c     = carácter de un octeto
f     = indicador lógico: cero significa «falso»; otro valor significa «cierto»
false = 0
ff    = indicador puro de Forth:
        0=«falso»; -1=«cierto»
        (-1 es un valor de 32 bitios con todos los bitios a uno)
i*x   = grupo de elementos sin especificar; puede estar vacío
j*x   = grupo de elementos sin especificar; puede estar vacío
n     = número de 32 bitios con signo
nt    = identificador de nombre de una palabra, notación de Gforth.
true  = -1 (valor de 32 bitios con todos los bitios a uno)
u     = número de 32 bitios sin signo
x     = valor sin determinar 32 bitios
xt    = identificador de ejecución de una palabra,
        notación de ANS Forth análoga a «cfa» en Forth clásico

Como es costumbre, los diferentes
elementos del mismo tipo se
distinguirán con un sufijo de un
dígito:

  ( a1 a2 a3 -- a2 xt )

O una «n» para el enésimo elemento de
una serie de longitud desconocida:

  ( u1 ... un n -- a )

En ciertos casos un apóstrofo sirve
para remarcar que el elemento de
salida es el mismo que el de entrada,
aunque modificado:

  ( a1 u1 -- a1' u1 )

[then]

\ }}} ##########################################################
\ Requisitos {{{

\ -----------------------------
\ De «Forth Foundation Library» (versión 0.8.0)
\ (http://code.google.com/p/ffl/)

\ Manejador de secuencias de escape de la consola:
s" ffl/trm.fs" required

\ -----------------------------
\ De programandala.net

require galope/sb.fs  \ Almacén circular de textos
\ ' bs+ alias s+
' bs& alias s&
\ ' bs" alias s" immediate
1024 heap_sb

require galope/random_strings.fs  \ Textos aleatorios
require galope/xy.fs  \ 'xy'
require galope/row.fs  \ 'row'
require galope/column.fs  \ 'column'
require galope/at-x.fs  \ 'at-x'
require galope/print.fs  \ Impresión de textos justificados
require galope/randomize.fs  \ 'randomize'
require galope/seconds.fs  \ 'seconds'
require galope/two-drops.fs  \ '2drops'
require galope/sconstant.fs  \ 'sconstant'

\ }}} ##########################################################
\ Herramientas {{{

: show  ( -- )
  cr .s
  ;
: wait  ( -- )
  key drop
  ;
: show...  ( -- )
  show wait
  ;
' true alias [true]  immediate
' false alias [false]  immediate
: period+  ( a1 u1 -- a2 u2 )
  \ Añade un punto al final de una cadena.
  s" ." s+
  ;
: comma+  ( a1 u1 -- a2 u2 )
  \ Añade una coma al final de una cadena.
  s" ," s+
  ;

\ }}} ##########################################################
\ Vocabularios {{{

vocabulary game_vocabulary  \ palabras del programa
vocabulary player_vocabulary  \ palabras del jugador
\ XXX no se usa todavía:
\ vocabulary answer_vocabulary  \ respuestas a preguntas de «sí» o «no»
vocabulary menu_vocabulary  \ palabras para las opciones del menú

: restore_vocabularies  ( -- )
  \ Restaura los vocabularios a su orden habitual.
  only forth also game_vocabulary definitions
  ;
restore_vocabularies

\ }}} ##########################################################
\ Pantalla {{{

: last_col  ( -- u )
  cols 1-
  ;
: last_row  ( -- u )
  rows 1-
  ;

false  [if]
\ XXX INFORMER:
cr ." rows x cols = " rows . cols .
cr ." last row = " last_row . 
cr ." last col = " last_col .
wait
[then]

: no_window  ( -- )
  \ Desactiva la definición de zona de pantalla como ventana.
  [char] r trm+do-csi0
  ;

: output_window  ( -- )
  \ Selecciona una zona de pantalla para la salida principal
  \ (todas las líneas salvo las dos últimas).
  \ Nótese que 'trm+set-scroll-region' cuenta las líneas empezando por uno,
  \ mientras que ANS Forth cuenta líneas y columnas empezando por cero.
  \ last_row 1- 1 trm+set-scroll-region
  last_row 1 - 1 trm+set-scroll-region
  \ 10 1 trm+set-scroll-region 
  ;
: at_first_output  ( -- )
  \ Sitúa el cursor en la posición en que se ha de imprimir la primera frase
  \ (en la parte inferior de la ventana de salida).
  \ 0 last_row 3 - at-xy
  \ 0 dup at-xy
  \ 0 9 at-xy  \ XXX TMP
  0 last_row 2 - at-xy
  ;
: init_output_cursor  ( -- )
  output_window
  at_first_output trm+save-current-state
  no_window
  ;
: at_input  ( -- )
  \ Sitúa el cursor en la zona de entrada (la última línea).
  0 last_row at-xy
  ;

\ }}} ##########################################################
\ Impresión de textos {{{

\ Indentación de la primera línea de cada párrafo (en caracteres):
2 value /indentation 
\ ¿Indentar también la línea superior de la pantalla?:
variable indent_first_line_too?

: not_first_line?  ( -- ff )
  row 0>
  ;
: indentation?  ( -- ff )
  \ ¿Indentar la línea actual?
  not_first_line? indent_first_line_too? @ or
  ;
: (indent)  ( -- )
  \ Indenta.
  /indentation print_indentation
  ;
: indent  ( -- )
  \ Indenta si es necesario.
  indentation? if  (indent)  then
  ;
: cr+  ( -- )
  print_cr indent 
  ;
: paragraph  ( a u -- )
  \ Imprime un texto justificado como inicio de un párrafo.
  \ a u = Texto
  cr+ print
  ;

\ }}} ##########################################################
\ Título {{{

: title_row  ( u -- u )
  \ Hace un salto de línea y sitúa el cursor en una columna.
  \ u = Columna
  dup cr at-x
  ;
: .title  ( -- )
  \ Imprime el título en la posición actual del cursor.
  column
  ."   _" title_row
  ."  /_)    _)_ _ ( _  o  _   _   _   _ o  _" title_row
  ." / / (_( (_ (_) ) ) ( )_) ) ) (_) (  ( (" title_row
  ."                     (            _)   _)" title_row
  drop
  ;

40 constant title_width 
4 constant title_height

: margin  ( u1 u2 -- u3 )
  \ Devuelve el margen que hay que dejar para centrar algo en pantalla.
  \ u1 = Medida grande, la de la pantalla (ancho o alto)
  \ u2 = Medida pequeña, la del texto a imprimir (ancho o alto)
  - 2 /
  ;
: .centered_title  ( -- )
  \ Imprime el título en el centro de la pantalla.
  cols title_width margin
  rows title_height margin
  at-xy .title
  ;
  
\ }}} ##########################################################
\ Datos {{{

variable #sentences  \ Número de frases (en Gforth se inicializa a cero)
defer 'sentences  \ Tabla de las direcciones de las frases

: >sentence>  ( u1 -- u2 )
  \ Convierte el número ordinal de una frase
  \ en su número de elemento en la tabla.
  #sentences @ swap -
  ;
: 'sentence  ( u -- a )
  \ Convierte el número ordinal de una frase en su dirección.
  >sentence> cells 'sentences + @
  ;
: sentence$  ( u -- a1 u1 )
  \ Devuelve una frase a partir de su número ordinal.
  'sentence count
  ;
: .sentence  ( u -- )
  \ Imprime una frase.
  \ u = Número ordinal de la frase 
  output_window trm+restore-current-state
  sentence$ paragraph trm+save-current-state
  no_window
  ;

: hs,  ( a u -- a1 )
  \ Compila una cadena en el diccionario y devuelve su dirección.
  here rot rot s,
  ;
: sentence:  ( a u "name" -- a1 )
  \ Compila una frase, devuelve su dirección
  \ y crea una constante que devolverá su número ordinal.
  hs,  #sentences 1 over +!  @ constant
  ;

\ Crear las frases, definidas en un fichero independiente:
include autohipnosis_sentences.fs

\ En este punto, las direcciones de todas las frases
\ están en la pila y la variable 'sentences#' contiene
\ el número de frases que han sido creadas.
( a1 ... an )

: sentences,  ( a1 ... an -- )
  \ Compila las direcciones de las frases.
  #sentences @ 0  ?do  ,  loop
  ;

\ Tabla para las direcciones de las frases
create ('sentences)
' ('sentences) is 'sentences
sentences,  \ Crear la tabla compilando sus datos

: associated?  ( u a | a u -- ff )
  \ ¿Está un término asociado a una frase?
  \ u = Identificador de la frase
  \ a = Dirección de la zona de datos de la palabra del término
  + c@ 0<>
  ;
: associate  ( u a -- )
  \ Asocia una frase a un término.
  \ u = Identificador de la frase
  \ a = Dirección de la zona de datos de la palabra del término.
  + true swap c!
  ;

: execute_nt  ( i*x nt -- j*x )
  \ Ejecuta el comportamiento en modo de interpretación
  \ de una palabra cuyo nt se proporciona.
  name>int execute
  ;
: execute_latest  ( i*x -- j*x )
  \ Ejecuta el comportamiento en modo de interpretación
  \ de la última palabra creada.
  latest execute_nt
  ;

: update_term  ( u nt -- )
  \ Asocia un término a una frase.
  \ u = Identificador de la frase
  \ nt = Identificador de nombre de la palabra del término.
  \ cr ." update_term ... " show \ XXX INFORMER
  execute_nt  ( u a )  associate 
  \ cr ." salida de update_term ... " show \ XXX INFORMER
  ;
\ Alias necesario porque el vocabulario 'forth' no estará
\ visible durante la creación de los términos:
' variable alias term
\ ' create alias variablx \ XXX try
\ XXX Si se usa 'create' en lugar de 'variable', se reproduce
\ el dichoso error.
: create_term_word  ( a u -- )
  \ Crea una palabra para el término.
  \ a u = Término
  \ cr ." create_term_header ... " show \ XXX INFORMER
  \ Sistema antiguo:
  \ name-too-short? header, reveal dovar: cfa, 

  \ 2012-06-17 Sistema nuevo:
  \ s" term" 2swap s& evaluate

  \ XXX funciona:
  \ Alternativa al sistema nuevo:
  \ also forth s" variable" 2swap s& evaluate previous

  \ XXX TMP:
  \ also forth s" variablx" 2swap s& evaluate previous

  \ XXX El error también se produce usando 'create' así:
  \ also forth s" create" 2swap s& evaluate previous
  \ cr ." salida de create_term_header ... " show \ XXX INFORMER

  \ 2012-09-11 Otro sistema:
  nextname term
  ;
: create_term_array  ( -- )
  \ Crea la matriz de datos de un término.
  \ Cada término tiene una matriz para marcar
  \ las frases con las que está asociado.
  \ Para simplificar el código, se usa una matriz de octetos
  \ en lugar de una matriz de bitios: tantos octetos
  \ como frases hayan sido definidas.
  here #sentences @ dup allot align erase
  ;
: create_term  ( a u -- )
  \ Crea un término;
  \ funciona como una variable
  \ pero tiene una zona de datos de tantos octetos
  \ como frases hayan sido definidas.
  \ a u = Término
  \ cr ." create_term entrada ( a u )" show \ XXX INFORMER
  create_term_word create_term_array
  \ cr ." salida de create_term ( ) " show \ XXX INFORMER
  ;
: init_term  ( u -- )
  \ Inicializa la última palabra del juego creada,
  \ asociándola a una frase.
  \ u = Identificador de la frase
  \ cr ." init_term entrada ( u ) " show \ XXX INFORMER
  execute_latest associate
  \ cr ." salida de init_term ( ) " show \ XXX INFORMER
  ;
: new_term  ( u a1 u1 -- )
  \ Crea un nuevo término asociado a una frase.
  \ u = Identificador de la frase
  \ a1 u1 = Término
  \ cr ." new_term entrada ( u a1 u1 ) " show \ XXX INFORMER
  create_term init_term
  \ cr ." new_term salida ( ) " show \ XXX INFORMER
  ;
: another_term  ( u a1 u1 -- )
  \ Crea o actualiza una palabra asociada a una frase.
  \ u = Identificador de la frase
  \ a1 u1 = Nombre de la palabra
  \ cr ." another_term entrada ( u a1 u1 ) " show \ XXX INFORMER
  2dup
  \ cr ." another_term before find-name " show \ XXX INFORMER
  find-name
  \ cr ." another_term after find-name " show \ XXX INFORMER
  ?dup 
  \ cr ." another_term tras ?dup " show \ XXX INFORMER
  if  nip nip update_term  else  new_term  then
  \ cr ." another_term final ( ) " show \ XXX INFORMER
  ;
: parse_term  ( -- a u )
  \ Devuelve, del flujo de código fuente, el siguiente término.
  begin   parse-name dup 0=
  while   2drop refill 0= abort" Error en el código fuente: falta un '}terms'" 
  repeat
  ;
: another_term?  ( -- a u f )
  \ ¿Hay otro término en la lista?
  \ Toma la siguiente palabra del flujo de código fuente
  \ y comprueba si es el final de la lista de términos asociados a una frase.
  \ a u = Palabra encontrada
  \ f = ¿No es el final de la lista?
  parse_term 2dup s" }terms" compare
  ;
: terms{  ( u "name#0" ... "name#n" "}terms" -- )
  \ Crea o actualiza palabras asociadas a una frase.
  \ u = Identificador de frase
  \ cr ." ############################# terms{" show \ XXX INFORMER
  only game_vocabulary also player_vocabulary definitions
  assert( depth 1 = )
  begin   
    \ cr ." terms{ after begin ... " show \ XXX INFORMER
    dup another_term? ( u u a1 u1 f )
    \ cr ." terms{ before while ... " show \ XXX INFORMER
    assert( depth 5 = )
  while   another_term
    assert( depth 1 = )
    \ cr ." terms{ before repeat... " show \ XXX INFORMER
  repeat  
  assert( depth 4 = )
  \ cr ." antes de 2drop 2drop ... " show \ XXX INFORMER
  2drop 2drop
  restore_vocabularies
  ;

\ Crear los términos, definidos en un fichero independiente:
include autohipnosis_terms.fs

\ Crear los comandos especiales para controlar el juego

also player_vocabulary definitions
: #fin ( -- )
  \ XXX TODO
  ;
restore_vocabularies

\ }}} ##########################################################
\ Intérprete de comandos {{{

variable sentence#  \ Frase en curso
variable valid  \ Contador de términos acertados en cada comando

: valid++  (  a -- )
  \ Incrementa la cuenta de aciertos, si procede.
  \ a = Dirección de la zona de datos de un término asociado a una frase
  sentence# @ associated? abs valid +!
  ;
: execute_term  ( nt -- )
  \ Executa un término asociado a una frase.
  \ nt = Identificador de nombre del término.
  execute_nt  ( a ) valid++
  ;
: (evaluate_command)  ( -- )
  \ Analiza la fuente actual con los vocabularios activos,
  \ ejecutando las palabras reconocidas
  \ como términos asociados a una frase.
  begin   
  \ cr ." (evaluate_command) 1 " show... \ XXX INFORMER
  parse-name ?dup
  \ cr ." (evaluate_command) 2 " show... \ XXX INFORMER
  while   
  \ cr ." (evaluate_command) 2a " show... \ XXX INFORMER
  find-name ?dup
  \ cr ." (evaluate_command) 3 " show... \ XXX INFORMER
  if  execute_term  then
  repeat  drop
  ;
: evaluate_command  ( a u -- )
  \ Analiza una cadena con el vocabulario del jugador.
  only player_vocabulary
  ['] (evaluate_command) execute-parsing
  restore_vocabularies
  ;
variable testing
: valid?  ( a u -- ff )
  \ ¿Contiene una cadena algún término asociado a la frase actual?
  valid off  evaluate_command  valid @ 0<> 
  testing @ or
  ;
s" > " sconstant prompt$
: /command  ( -- u )
  \ Longitud máxima de un comando
  cols prompt$ nip - 1-
  ;
: init_command_line  ( -- )
  \ Limpia la línea de comandos y sitúa el cursor.
  0 last_row at-xy trm+erase-line
  ;
create 'command /command chars allot align
: (command)  ( -- a u )
  \ Acepta un comando del jugador.
  'command /command accept  'command swap
  ;
: .prompt  ( -- )
  prompt$ type
  ;
: command  ( -- a u )
  \ Prepara la pantalla y acepta un comando del jugador.
  init_command_line .prompt (command)
  ;

\ }}} ##########################################################
\ Final {{{

variable success?  \ ¿Se ha completado con éxito el juego?

: the_happy_end  ( -- )
  \ XXX TODO
  success? on
  ;

\ }}} ##########################################################
\ Ayuda {{{

: curiosities  ( -- )
  \ XXX TODO
  s" Curiosidades..." paragraph
  ;

: game$  ( -- a u )
  s{ s" juego" s" programa" }s
  ;
: the_game$  ( -- a u )
  s" el" game$ s&
  ;
: except$  ( -- a u )
  s{ s" excepto" s" salvo" }s
  ;
: way$  ( -- a u )
  s{ s" manera" s" forma" }s
  ;
: leave$  ( -- a u )
  s{ s" abandonar" s" detener" s" dejar" s" interrumpir" }s
  ;
: left$  ( -- a u )
  s{ s" abandonado" s" detenido" s" dejado" s" interrumpido" }s
  ;
: pressing$  ( -- a u )
  s{ s" pulsando" s" mediante" }s
  ;
: they_make$  ( -- a u )
  s{ s" forman" s" componen" }s
  ;
: instructions_0  ( -- )
  \ Instrucciones sobre el objeto del juego.
  s{ s" El" s" Este" }s
  s{ s" programa" s" juego" }s&
  s{ s" mostrará" s" imprimirá" }s&
  s" un texto" s& s" en la pantalla" s?&
  s" y" s&
  s{ s" a continuación" s" después" s" seguidamente" }s?&
  s{ s" esperará" s" se quedará esperando" }s&
  s" una respuesta."  s&
  s{
    s" El" s{ s" juego" s" objetivo" }s& s" consiste en" s&
    s" Lo que" s{ s" has de" s" tienes que" s" hay que" s" debes" }s& s" hacer es" s&
    s" El jugador" s{ s" debe" s" tiene que" }s&
    s{ s" Tienes que" s" Debes" s" Has de" s" Hay que" }s
    s" Tu" s{ s" objetivo" s" misión" }s& s{ s" es" s" será" s" consiste en" }s&
  }s&
  s{ s" responder a" s" escribir una respuesta para" }s&
  s" cada texto," s&
  s{ s" usando" s" empleando" s" utilizando" s" incluyendo" }s&
  s{ s" como mínimo" s" al menos" s" por lo menos" }s&
  s" un sustantivo" s& s" (en singular)" s?&
  s{ s" relacionado" s" que tenga relación" }s& s" con" s&
  s{ s" el mismo" s" él" }s& comma+
  s" pero que no sea" s&
  s{ s" familia de" s" de la misma familia que" }s&
  s" alguna de las palabras" s&
  s{ 
  s" que lo" they_make$ s& 
  s" que" they_make$ s&{ s" el" s" dicho" }s& s" texto" s&
  s{ s" del" s" de dicho" }s s" texto" s&
  }s& period+
  s" El proceso" s&
  s{  s" se repetirá" s" continuará"
      s" no acabará" s" no terminará" 
      s" seguirá" s" durará"
  }s&
  s" hasta que todos los textos hayan sido mostrados y respondidos." s&
  paragraph
  ;
: instructions_1  ( -- )
  \ Instrucciones sobre el abandono del juego.
  s{
  s" No es posible" leave$ s& the_game$ s& comma+ except$ s& pressing$ s&
  s" El" game$ s& s" no puede ser" s& left$ s& comma+ except$ s& pressing$ s&
  s" La única" way$ s& s" de" s& leave$ s& the_game$ s& s" es pulsar" s&
  }s
  s{ s" la combinación de teclas" s" el atajo de teclado" s" las teclas" }s&
  s" «Ctrl»+«C»," s&
  s{ s" lo que" s" lo cual" }s& s" te" s&
  s{ s" devolverá" s" hará regresar" }s&
  s{ s" a la línea de comandos" s" al intérprete" }s&
  s" de Forth." s&
  \ XXX FIXME -- Ctrl+C return to the OS shell
  paragraph
  ;
: instructions_2  ( -- )
  \ Instrucciones sobre el arranque del juego.
  s" Tanto para empezar a jugar ahora como para hacerlo tras haber"
  left$ s& the_game$ s&
  s" puedes" s& s{ s" usar" s" probar" }s&
  s" cualquier palabra que" s&
  s{ s" se te ocurra" s" te parezca" }s& comma+ s" hasta" s&
  s{ s" encontrar" s" dar con" s" acertar con" }s&
  s{ s" alguna" s" una" }s& s" que" s&
  s{ s" surta efecto" s" funcione" s" sirva" }s& period+
  paragraph
  ;
: instructions  ( -- false )
  \ XXX inacabado
  page s" Instrucciones de Autohipnosis" paragraph cr cr
  instructions_0
  instructions_1
  instructions_2  false
  ;

\ }}} ##########################################################
\ Menú {{{

: menu_0$  ( -- a u )
  \ Primera versión del texto del «menú».
  s" Qué"
  s{ s{ s" quieres" s" deseas" }s s" hacer" s?&
  s" ordenas" }s&
  ;
: menu_1$  ( -- a u )
  \ Segunda versión del texto del «menú».
  s" Cuáles son tus"
  s{ s" órdenes" s" instrucciones" }s&
  ;
: menu$  ( -- a u )
  \ Texto del «menú».
  s" ¿" s{ menu_0$ menu_1$ }s+ s" ?" s+
  ;
: .menu  ( -- )
  \ Imprime el «menú».
  menu$ paragraph
  ;
: (evaluate_option)  ( -- )
  \ Analiza la fuente actual con los vocabularios activos,
  \ ejecutando las palabras reconocidas.
  begin   parse-name ?dup
  while   find-name ?dup   if  execute_nt  then
  repeat  drop
  ;
: evaluate_option  ( a u -- )
  \ Analiza una cadena con el vocabulario del menú. 
  only menu_vocabulary
  ['] (evaluate_option) execute-parsing
  restore_vocabularies
  ;
variable finished
: finish  finished on
  ;
: menu  ( -- ff )
  \ Muestra el menú, espera y obedece una opción.
  \ ff = ¿Salir del programa?
  finished off
  .menu command evaluate_option finished @
  ;

\ }}} ##########################################################
\ Inicialización {{{

: init/once  ( -- )
  \ Inicialización necesaria antes de la primera partida.
  \ XXX TODO ?
  page
  ;
: init/game  ( -- )
  \ Inicialización necesaria antes de cada partida.
  page .centered_title 10 seconds
  init_output_cursor  page
  ;

\ }}} ##########################################################
\ Juego {{{

: ask  ( u -- )
  \ Pide al jugador un comando, hasta que recibe uno válido.
  \ u = Número ordinal de la frase actual.
  sentence# !
  begin  command  valid?  until
  ;
: step  ( u -- )
  \ Un paso del juego.
  \ u = Número ordinal de la frase actual.
  dup .sentence ask
  ;
: game  ( -- )
  \ Bucle de cada partida.
  #sentences @ dup 1
  do  i step  loop
  .sentence  \ Frase final
  the_happy_end
  ;
: play  ( -- )
  \ Jugar una partida.
  init/game game   ;

\ }}} ##########################################################
\ Comandos del menú {{{

\ El menú tiene solo tres comandos, cada uno con muchos sinónimos.

also menu_vocabulary definitions

\ Comando «instrucciones»:
' instructions alias aclaración
' instructions alias ayuda
' instructions alias espera
' instructions alias esperad
' instructions alias esperar
' instructions alias espero
' instructions alias ex  \ examina
' instructions alias examina
' instructions alias examinad
' instructions alias examinar
' instructions alias examino
' instructions alias examínate
' instructions alias examínome
' instructions alias explicación
' instructions alias i  \ inventario
' instructions alias instrucciones
' instructions alias inventario
' instructions alias lee
' instructions alias leed
' instructions alias leer
' instructions alias leo
' instructions alias m  \ mirar
' instructions alias manual
' instructions alias mira
' instructions alias mirad
' instructions alias mirar
' instructions alias miro
' instructions alias mírate
' instructions alias mírome
' instructions alias pista
' instructions alias pistas
' instructions alias registra
' instructions alias registrad
' instructions alias registrar
' instructions alias registro
' instructions alias salida
' instructions alias salidas
' instructions alias x  \ salidas
\ Comando «jugar»:
' play alias abajo
' play alias arranca
' play alias arrancad 
' play alias arrancar 
' play alias arranco
' play alias arriba
' play alias baja
' play alias bajad
' play alias bajar
' play alias bajo
' play alias comenzad
' play alias comenzar
' play alias comienza
' play alias comienzo
' play alias e  \ este
' play alias ejecuta
' play alias ejecutad
' play alias ejecutar
' play alias ejecuto
' play alias empezad
' play alias empezar
' play alias empieza
' play alias empiezo
' play alias entra
' play alias entrad
' play alias entrar
' play alias entro
' play alias este
' play alias inicia
' play alias iniciad 
' play alias iniciar 
' play alias inicio
' play alias juega
' play alias juego
' play alias jugad
' play alias jugar
' play alias n  \ norte
' play alias ne  \ noreste
' play alias no  \ noroeste
' play alias noreste
' play alias noroeste
' play alias norte
' play alias o  \ oeste
' play alias oeste
' play alias partida
' play alias probad
' play alias probar
' play alias pruebo
' play alias s  \ sur
' play alias se  \ sureste
' play alias so  \ suroeste
' play alias sube
' play alias subid
' play alias subir
' play alias subo
' play alias sudeste
' play alias sudoeste
' play alias sur
' play alias sureste
' play alias suroeste
\ Comando «fin»:
' finish alias acaba
' finish alias acabad
' finish alias acabar
' finish alias acabo
' finish alias acabose
' finish alias acabó
' finish alias adiós
' finish alias apaga
' finish alias apagad
' finish alias apagar
' finish alias apago
' finish alias apagón
' finish alias apágate
' finish alias cerrad
' finish alias cerrar
' finish alias cierra
' finish alias cierre
' finish alias cierro
' finish alias ciérrate
' finish alias concluid
' finish alias concluir
' finish alias conclusión
' finish alias concluye
' finish alias concluyo
' finish alias desconecta
' finish alias desconectad
' finish alias desconectar
' finish alias desconecto
' finish alias desconexión
' finish alias desconéctate
' finish alias fin
' finish alias final
' finish alias finaliza
' finish alias finalización
' finish alias finalizad
' finish alias finalizar
' finish alias finalizo
' finish alias sal
' finish alias salgo
' finish alias salid
' finish alias salida
' finish alias salir
' finish alias termina
' finish alias terminación
' finish alias terminad
' finish alias terminar
' finish alias termino
' finish alias término

restore_vocabularies

\ }}} ##########################################################
\ Principal {{{

: farewell$  ( -- a u )
  s{  s" Adiós"
      s" Hasta" s{ s" otra" s" la vista" s" pronto" s" luego" s" más ver" }s&
  }s  period+
  ;
: farewell  ( -- )
  page farewell$ paragraph space 2 seconds bye
  ;

: main  ( -- )
  \ Bucle principal del juego.
  init/once  begin  menu  until  farewell
  ;

' main alias autohipnosis
' main alias abajo
' main alias arranca
' main alias arrancad
' main alias arrancar 
' main alias arranco
' main alias arriba
' main alias baja
' main alias bajad
' main alias bajar
' main alias bajo
' main alias comenzad
' main alias comenzar
' main alias comienza
' main alias comienzo
' main alias e  \ este
' main alias ejecuta
' main alias ejecutad
' main alias ejecutar
' main alias ejecuto
' main alias ejecútate
' main alias empezad
' main alias empezar
' main alias empieza
' main alias empiezo
' main alias entra
' main alias entrad
' main alias entrar
' main alias entro
' main alias este
' main alias inicia
' main alias iniciad
' main alias iniciar 
' main alias inicio
' main alias iníciate
' main alias juega
' main alias juego
' main alias jugad
' main alias jugar
' main alias n  \ norte
' main alias ne  \ noreste
' main alias no  \ noroeste
' main alias noreste
' main alias noroeste
' main alias norte
' main alias o  \ oeste
' main alias oeste
' main alias partida
' main alias probad
' main alias probar
' main alias prueba
' main alias pruebo
' main alias s  \ sur
' main alias se  \ sureste
' main alias so  \ suroeste
' main alias sube
' main alias subid
' main alias subir
' main alias subo
' main alias sudeste
' main alias sudoeste
' main alias sur
' main alias sureste
' main alias suroeste
' main alias adelante

\ }}} ##########################################################
\ Herramientas para depuración {{{

true [if]

: .sentences  ( a -- )
  \ Imprime todas las frases con las que está asociado un término.
  \ a = Dirección del término
  \ Ejemplos de uso:
  \   confort .sentences
  \   cambio .sentences
  \ Evidentemente, antes el vocabulario 'player_vocabulary' debe estar activo:
  \   also player_vocabulary
  cr
  #sentences @ 0 ?do
    dup i + c@ if  i sentence$ type cr  then
  loop  drop
  ;

\ Crear el término «z», válido para todas las frases:
also player_vocabulary definitions
0 s" z" new_term
z #sentences @ true fill
restore_vocabularies

[then]

\ }}} ##########################################################
\ Notas {{{

false  [if]

Ideas:

Modo de juego por puntos, siempre avanzando, un punto por
acierto.

XXX TODO:

Pedir confirmación de salida, pulsando la barra espaciadora.

[then]

\ }}} ##########################################################

autohipnosis
