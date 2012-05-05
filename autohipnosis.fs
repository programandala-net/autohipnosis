\ autohipnosis.fs

\ Fichero principal de
\ «Autohipnosis» (versión A-00-2012050521),
\ un juego conversacional experimental.

\ http://programandala.net/es.programa.autohipnosis

\ Copyright (C) 2012 Marcos Cruz (programandala.net)

\ Autohipnosis is free software; you can redistribute it
\ and/or modify it under the terms of the GNU General Public
\ License as published by the Free Software Foundation;
\ either version 2 of the License, or (at your option) any
\ later version.

\ Autohipnosis is distributed in the hope that it will be
\ useful, but WITHOUT ANY WARRANTY; without even the implied
\ warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
\ PURPOSE.  See the GNU General Public License for more
\ details.

\ You should have received a copy of the GNU General Public
\ License along with this program; if not, see
\ http://www.gnu.org/licenses .

\ Este programa está escrito en Forth usando el sistema Gforth:
\ http://www.jwdt.com/~paysan/gforth.html
\ Para escribir este programa se ha empleado el editor Vim:
\ http://www.vim.org

\ Historial de desarrollo:
\ http://programandala.net/es.programa.autohipnosis.historial

\ Información sobre juegos conversacionales:
\ http://caad.es
 
\ }}} ##########################################################
\ Notación de la pila {{{

(

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
        [-1 es un valor de 32 bitios con todos los bitios a uno]
i*x   = grupo de elementos sin especificar; puede estar vacío
j*x   = grupo de elementos sin especificar; puede estar vacío
n     = número de 32 bitios con signo
nt    = identificador de nombre de una palabra, notación de Gforth.
true  = -1 [valor de 32 bitios con todos los bitios a uno]
u     = número de 32 bitios sin signo
x     = valor sin determinar 32 bitios
xt    = identificador de ejecución de una palabra,
        notación de ANS Forth análoga a «cfa» en Forth clásico

Como es costumbre, los diferentes elementos del mismo tipo
se distinguirán con un sufijo, casi siempre un dígito,
o bien un apóstrofo, según los casos.

)

\ }}} ##########################################################
\ Requisitos {{{

\ -----------------------------
\ De Gforth

s" random.fs" required
: randomize
  \ Reinicia la semilla de generación de números aleatorios.
  time&date 2drop 2drop * seed !  ;

s" debug.fs" required

\ -----------------------------
\ De «Forth Foundation Library» (versión 0.8.0)
\ (http://code.google.com/p/ffl/)

\ Cadenas de texto dinámicas:
s" ffl/str.fs" required
\ Manejador de secuencias de escape de la consola:
s" ffl/trm.fs" required

\ -----------------------------
\ De programandala.net

s" ghoul/sb.fs" required  \ Almacén circular de textos.
\ ' bs+ alias s+
' bs& alias s&
\ ' bs" alias s" immediate
1024 heap_sb

s" ghoul/xy.fs" required  \ 'xy', 'column', 'row'.
s" ghoul/random_strings.fs" required  \ Textos aleatorios.


\ }}} ##########################################################
\ Herramientas {{{

: show  cr .s  ;
: wait  key drop  ;
: show...  show wait  ;
' true alias [true]  immediate
' false alias [false]  immediate
[undefined] ++ [if]
  : ++  ( a -- )
    \ Incrementa el contenido de una dirección de memoria.
    1 swap +!  ;
[then]
[undefined] -- [if]
  : --  ( a -- )
    \ Decrementa el contenido de una dirección de memoria.
    -1 swap +!  ;
[then]
: period+  ( a1 u1 -- a2 u2 )
  \ Añade un punto al final de una cadena.
  s" ." s+  ;
: comma+  ( a1 u1 -- a2 u2 )
  \ Añade una coma al final de una cadena.
  s" ," s+  ;

\ }}} ##########################################################
\ Vocabularios {{{

vocabulary game_vocabulary  \ palabras del programa
: restore_vocabularies
  \ Restaura los vocabularios a su orden habitual.
  only forth also game_vocabulary definitions  ;
restore_vocabularies

false  [if]

  \ El método que sigue provoca errores de acceso a
  \ memoria al crear palabras en el vocabulario. No sé por
  \ qué.

  : case_sensitive_vocabulary  ( "name" -- )
    \ Crea un vocabulario con nombre y sensible a mayúsculas.
    create  table , 
    does>  ( pfa -- )
      \ Reemplaza el vocabulario superior.
      \ Código tomado de Gforth (compat/vocabulary.fs).
      @ >r
      get-order dup 0= 50 and throw  \ Error 50 («search-order underflow») si la lista está vacía
      nip r> swap set-order
    ;
    case_sensitive_vocabulary player_vocabulary  \ Palabras del jugador

[else]

  \ Método alternativo.

  table value (player_vocabulary)
  : player_vocabulary
    \ Reemplaza el vocabulario superior con el del jugador.
    \ Código adaptado de Gforth (compat/vocabulary.fs).
    get-order dup 0= 50 and throw  \ Error 50 («search-order underflow») si la lista está vacía.
    nip (player_vocabulary) swap set-order
    ;

[then]

\ vocabulary player_vocabulary  \ palabras del jugador
\ No se usan todavía!!!:
\ vocabulary answer_vocabulary  \ respuestas a preguntas de «sí» o «no»
vocabulary menu_vocabulary  \ palabras para las opciones del menú

\ }}} ##########################################################
\ Pantalla {{{

: last_col  ( -- u )  cols 1-  ;
: last_row  ( -- u )  rows 1-  ;

false  [if]
\ Debug!!!:
cr ." rows x cols = " rows . cols .
cr ." last row = " last_row . 
cr ." last col = " last_col .
wait
[then]

: no_window
  \ Desactiva la definición de zona de pantalla como ventana.
  [char] r trm+do-csi0  ;

: output_window
  \ Selecciona una zona de pantalla para la salida principal
  \ (todas las líneas salvo las dos últimas).
  \ Nótese que TRM+SET-SCROLL-REGION cuenta las líneas empezando por uno,
  \ mientras que ANS Forth cuenta líneas y columnas empezando por cero.
  last_row 3 - 1 trm+set-scroll-region  ;
2variable output-xy  \ Coordenadas del cursor en la ventana de salida
: save_output_cursor
  \ Guarda la posición actual del cursor en la ventana de salida.
  xy output-xy 2!  ;
: restore_output_cursor
  \ Restaura la posición guardada del cursor en la ventana de salida.
  output-xy 2@ at-xy  ;
: at_first_output
  \ Sitúa el cursor en la posición en que se ha de imprimir la primera frase
  \ (en la parte inferior de la ventana de salida).
  \ 0 last_row 5 - at-xy  ;
  0 dup at-xy  ;
: init_output_cursor
  at_first_output save_output_cursor  ;
: at_input
  \ Sitúa el cursor en la zona de entrada (la última línea).
  0 last_row at-xy  ;

\ }}} ##########################################################
\ Impresión de textos {{{

\ Cadena dinámica para impresión

(

Usamos una cadena dinámica llamada PRINT_STR para guardar
los textos que hay que mostrar justificados en pantalla. En
esta sección creamos la cadena y palabras útiles para
manipularla.

Nota!!!: La mayoría de estas palabras no son necesarias
en el programa. Habrá que borrarlas.

)

str-create print_str

: «»-clear
  \ Vacía la cadena dinámica PRINT_STR.
  print_str str-clear  ;
: «»!  ( a u -- )
  \ Guarda una cadena en la cadena dinámica PRINT_STR.
  print_str str-set  ;
: «»@  ( -- a u )
  \ Devuelve el contenido de la cadena dinámica PRINT_STR.
  print_str str-get  ;
: «+  ( a u -- )
  \ Añade una cadena al principio de la cadena dinámica PRINT_STR.
  print_str str-prepend-string  ;
: »+  ( a u -- )
  \ Añade una cadena al final de la cadena dinámica PRINT_STR.
  print_str str-append-string  ;
: «c+  ( c -- )
  \ Añade un carácter al principio de la cadena dinámica PRINT_STR.
  print_str str-prepend-char  ;
: »c+  ( c -- )
  \ Añade un carácter al final de la cadena dinámica PRINT_STR.
  print_str str-append-char  ;
: «»bl+?  ( u -- ff )
  \ ¿Se debe añadir un espacio al concatenar una cadena a la cadena dinámica PRINT_STR ?
  \ u = Longitud de la cadena que se pretende unir a la cadena dinámica PRINT_STR
  0<> print_str str-length@ 0<> and  ;
: »&  ( a u -- )
  \ Añade una cadena al final de la cadena dinámica TXT, con un espacio de separación.
  dup «»bl+?  if  bl »c+  then  »+  ;
: «&  ( a u -- )
  \ Añade una cadena al principio de la cadena dinámica TXT, con un espacio de separación.
  dup «»bl+?  if  bl «c+  then  «+   ;

\ Impresión de párrafos justificados

variable #lines  \ Número de línea del texto que se imprimirá

\ Indentación de la primera línea de cada párrafo (en caracteres):
2 constant default_indentation  \ Predeterminada 
8 constant max_indentation  \ Máxima
variable /indentation  \ En curso

: not_first_line?  ( -- ff )  row 0>  ;
variable indent_first_line_too?  \ ¿Se indentará también la línea superior de la pantalla, si un párrafo empieza en ella?
: indentation?  ( -- ff )
  \ ¿Indentar la línea actual?
  not_first_line? indent_first_line_too? @ or  ;
: char>string  ( c u -- a u )
  \ Crea una cadena repitiendo un carácter.
  \ c = Carácter
  \ u = Longitud de la cadena
  \ a = Dirección de la cadena
  dup sb_allocate swap 2dup 2>r  rot fill  2r>  ;
: indentation+
  \ Añade indentación ficticia (con un carácter distinto del espacio)
  \ a la cadena dinámica PRINT_STR , si la línea del cursor no es la primera.
  indentation?  if
    [char] X /indentation @ char>string «+
  then  ;
: indentation-  ( a1 u1 -- a2 u2 )
  \ Quita a una cadena tantos caracteres por la izquierda como el valor de la indentación.
  /indentation @ -  swap /indentation @ +  swap  ;
: indent
  \ Mueve el cursor a la posición requerida por la indentación.
  /indentation @ ?dup  if  trm+move-cursor-right  then  ;
: indentation>  ( a1 u1 -- a2 u2 ) \ Prepara la indentación de una línea
  indentation?  if  indentation- indent  then  ;
: .line  ( a u -- )  cr type  ;
: .lines  ( a1 u1 ... an un n -- )
  \ Imprime n líneas de texto.
  \ a1 u1 = Última línea de texto
  \ an un = Primera línea de texto
  \ n = Número de líneas de texto en la pila
  dup #lines !  0  ?do  .line  loop  ;
: (paragraph)
  \ Imprime la cadena dinámica PRINT_STR ajustándose al ancho de la pantalla.
  indentation+  \ Añade indentación ficticia
  print_str str-get cols str+columns  \ Divide la cadena dinámica PRINT_STR 
  >r indentation> r>  \ Prepara la indentación efectiva de la primera línea
  .lines  \ Imprime las líneas
  print_str str-init  \ Vacía la cadena dinámica
  ;
: paragraph/ ( a u -- )
  \ Imprime una cadena ajustándose al ancho de la pantalla.
  print_str str-set (paragraph)  ;
: paragraph  ( a u -- )
  \ Imprime una cadena ajustándose al ancho de la pantalla;
  \ y un salto de línea final.
  \ dup >r  paragraph/ r> ?cr  \ Versión original!!!
  paragraph/ cr  ;

\ }}} ##########################################################
\ Pausas {{{

: time?  ( d -- ff )  utime d<  ;
: microseconds  ( u -- )
  \ Espera un número de microsegundos o hasta que se pulse una tecla.
  s>d utime d+
  begin  2dup time? key? 0= or  until
  begin  2dup time? key? or  until
  2drop  ;
: miliseconds  ( u -- )  1000 * microseconds  ;
: seconds  ( u -- )  1000000 * microseconds  ;

\ }}} ##########################################################
\ Título {{{

: title_row  ( u -- u )
  \ Hace un salto de línea y sitúa el cursor en una columna.
  \ u = Columna
  dup cr at-x  ;
: .title
  \ Imprime el título en la posición actual del cursor.
  column
  ."   _" title_row
  ."  /_)    _)_ _ ( _  o  _   _   _   _ o  _" title_row
  ." / / (_( (_ (_) ) ) ( )_) ) ) (_) (  ( (" title_row
  ."                     (            _)   _)" title_row
  drop  ;

40 constant title_width 
4 constant title_height

: margin  ( u1 u2 -- u3 )
  \ Devuelve el margen que hay que dejar para centrar algo en pantalla.
  \ u1 = Medida grande, la de la pantalla (ancho o alto)
  \ u2 = Medida pequeña, la del texto a imprimir (ancho o alto)
  - 2 /  ;
: .centered_title
  \ Imprime el título en el centro de la pantalla.
  cols title_width margin
  rows title_height margin
  at-xy .title  ;
  
\ }}} ##########################################################
\ Datos {{{

variable #sentences  \ Número de frases (en Gforth se inicializa a cero)
defer 'sentences  \ Tabla de las direcciones de las frases

: >sentence>  ( u1 -- u2 )
  \ Convierte el número ordinal de una frase
  \ en su número de elemento en la tabla.
  #sentences @ swap -  ;
: 'sentence  ( u -- a )
  \ Convierte el número ordinal de una frase en su dirección.
  >sentence> cells 'sentences + @  ;
: sentence$  ( u -- a1 u1 )
  \ Devuelve una frase a partir de su número ordinal.
  'sentence count  ;
: .sentence  ( u -- )
  \ Imprime una frase.
  \ u = Número ordinal de la frase 
  output_window
  restore_output_cursor
  sentence$ paragraph
  save_output_cursor
  no_window
  ;

: hs,  ( a u -- a1 )
  \ Compila una cadena en el diccionario y devuelve su dirección.
  here rot rot s,  ;
: sentence:  ( a u "name" -- a1 )
  \ Compila una frase, devuelve su dirección
  \ y crea una constante que devolverá su número ordinal.
  hs,  #sentences 1 over +!  @ constant  ;

\ Crear las frases, definidas en un fichero independiente:
s" autohipnosis_sentences.fs" included

\ En este punto, las direcciones de todas las frases
\ están en la pila y la variable SENTENCES# contiene
\ el número de frases que han sido creadas.
( a1 ... an )

: sentences,  ( a1 ... an -- )
  \ Compila las direcciones de las frases.
  #sentences @ 0  do  ,  loop  ;

\ Tabla para las direcciones de las frases
create ('sentences)  ' ('sentences) is 'sentences
sentences,  \ Rellenar la tabla compilando en el diccionario su contenido

: associated?  ( u a | a u -- ff )
  \ ¿Está una palabra del juego asociada a una frase?
  \ u = Identificador de la frase
  \ a = Dirección de la zona de datos de la palabra
  + c@ 0<>  ;
: associate  ( u a -- )
  \ Asocia una frase a una palabra del juego.
  \ u = Identificador de la frase
  \ a = Dirección de la zona de datos de la palabra
  + true swap c!  ;

: execute_nt  ( i*x nt -- j*x )
  \ Ejecuta el comportamiento en modo de interpretación
  \ de una palabra cuyo nt se proporciona.
  name>int execute  ;
: execute_latest  ( i*x -- j*x )
  \ Ejecuta el comportamiento en modo de interpretación
  \ de la última palabra creada.
  latest execute_nt  ;

: update_term  ( u nt -- )
  \ Asocia una palabra del juego a una frase.
  \ u = Identificador de la frase
  \ nt = Identificador de nombre de la palabra
  \ cr ." update_term ... " show \ depuración!!!
  execute_nt  ( u a )  associate 
  \ cr ." salida de update_term ... " show \ depuración!!!
  ;
: create_term_header  ( a u -- )
  \ Crea la cabecera de una palabra del juego.
  \ a u = Nombre de la palabra
  \ cr ." create_term_header ... " show \ depuración!!!
  name-too-short? header, reveal dovar: cfa, 
  \ cr ." salida de create_term_header ... " show \ depuración!!!
  ;
: create_term_array
  \ Crea la matriz de datos de una palabra del juego.
  \ Cada palabra del juego tiene una matriz para marcar
  \ las frases con las que está asociada.
  \ Para simplificar el código, se usa una matriz de octetos
  \ en lugar de una matriz de bitios: tantos octetos
  \ como frases hayan sido definidas.
  here  #sentences @ dup allot align  erase  ;
: (create_term)  ( a u -- )
  \ Crea una palabra asociada a una frase,
  \ que funciona como una variable
  \ pero que tiene una zona de datos de tantos octetos
  \ como frases hayan sido definidas.
  \ a u = Nombre de la palabra
  \ cr ." (create_term) entrada ( a u )" show \ depuración!!!
  create_term_header create_term_array
  \ cr ." salida de (create_term) ( ) " show \ depuración!!!
  ;
: init_term  ( u -- )
  \ Inicializa la última palabra del juego creada,
  \ asociándola a una frase.
  \ u = Identificador de la frase
  \ cr ." init_term entrada ( u ) " show \ depuración!!!
  execute_latest associate
  \ cr ." salida de init_term ( ) " show \ depuración!!!
  ;
: create_term  ( u a1 u1 -- )
  \ Crea e inicializa una palabra asociada a una frase.
  \ u = Identificador de la frase
  \ a1 u1 = Nombre de la palabra
  \ cr ." create_term entrada ( u a1 u1 ) " show \ depuración!!!
  (create_term) init_term
  \ cr ." create_term salida ( ) " show \ depuración!!!
  ;
: another_term  ( u a1 u1 -- )
  \ Crea o actualiza una palabra asociada una frase.
  \ u = Identificador de la frase
  \ a1 u1 = Nombre de la palabra
  \ cr ." another_term entrada ( u a1 u1 ) " show \ depuración!!!
  2dup
  \ cr ." another_term before find-name " show \ depuración!!!
  find-name
  \ cr ." another_term after find-name " show \ depuración!!!
  ?dup 
  \ cr ." another_term tras ?dup " show \ depuración!!!
  if  nip nip update_term  else  create_term  then
  \ cr ." another_term final ( ) " show \ depuración!!!
  ;
: parse_term  ( -- a u )
  \ Devuelve la siguiente palabra asociada a una frase.
  begin   parse-name dup 0=
  while   2drop refill 0= abort" Error en el código fuente: falta un }terms" 
  repeat  ;
: another_term?  ( -- a u f )
  \ ¿Hay una nueva palabra en la lista?
  \ Toma la siguiente palabra en el flujo de entrada
  \ y comprueba si es el final de la lista de palabras asociadas a una frase.
  \ a u = Palabra encontrada
  \ ff = ¿No es el final de la lista?
  parse_term 
  \ 2dup cr ." ************************** " type  \ depuración!!!
  2dup s" }terms" compare
  \ cr ." ( -- a u f )" \ !!!
  ;
: terms{  ( u "name#0" ... "name#n" "}terms" -- )
  \ Crea o actualiza palabras asociadas a una frase.
  \ u = Identificador de frase
  \ cr ." ############################# terms{" show \ depuración!!!
  also player_vocabulary definitions
  assert( depth 1 = )
  begin   
  \ cr ." terms{ after begin ... " show \ depuración!!!
  dup another_term? ( u u a1 u1 f )
  \ cr ." terms{ before while ... " show \ depuración!!!
  assert( depth 5 = )
  while   another_term
  assert( depth 1 = )
    \ cr ." terms{ before repeat... " show \ depuración!!!
  repeat  
  assert( depth 4 = )
  \ cr ." antes de 2drop 2drop ... " show \ depuración!!!
  2drop 2drop
  restore_vocabularies  ;

\ Crear los términos, definidos en un fichero independiente:
s" autohipnosis_terms.fs" included

\ Crear los comandos especiales para controlar el juego

also player_vocabulary definitions
: FIN ( -- )
  \ Pendiente!!!
  ;
restore_vocabularies

\ }}} ##########################################################
\ Intérprete de comandos {{{

variable sentence#  \ Frase en curso
variable valid  \ Contador de términos acertados en cada comando

: valid++  (  a -- )
  \ Incrementa la cuenta de aciertos, si procede.
  \ a = Dirección de la zona de datos de un término asociado a una frase
  sentence# @ associated? abs valid +!  ;
: execute_term  ( nt -- )
  \ Executa un término asociado a una frase.
  \ nt = Identificador de nombre del término
  execute_nt  ( a ) valid++  ;
: (evaluate_command)
  \ Analiza la fuente actual con los vocabularios activos,
  \ ejecutando las palabras reconocidas
  \ como términos asociados a una frase.
  begin   
  \ cr ." (evaluate_command) 1 " show... \ depuración!!!
  parse-name ?dup
  \ cr ." (evaluate_command) 2 " show... \ depuración!!!
  while   
  \ cr ." (evaluate_command) 2a " show... \ depuración!!!
  find-name ?dup
  \ cr ." (evaluate_command) 3 " show... \ depuración!!!
  if  execute_term  then
  repeat  drop  ;
: evaluate_command  ( a u -- )
  \ Analiza una cadena con el vocabulario del jugador.
  only player_vocabulary
  ['] (evaluate_command) execute-parsing
  restore_vocabularies  ;
variable testing
: valid?  ( a u -- ff )
  \ ¿Contiene una cadena algún término asociado a la frase actual?
  valid off  evaluate_command  valid @ 0<> 
  testing @ or  ;
: /command  ( -- u )
  \ Longitud máxima de un comando
  cols  ;
: init_command_line
  \ Limpia la línea de comandos y sitúa el cursor.
  0 last_row at-xy trm+erase-line  ;
create 'command /command chars allot align
: command  ( -- a u )
  \ Acepta un comando del jugador.
  init_command_line
  'command /command accept  'command swap
  no_window  ;

\ }}} ##########################################################
\ Final {{{

variable success?  \ ¿Se ha completado con éxito el juego?

: the_happy_end
  \ Pendiente!!!
  success? on  ;

\ }}} ##########################################################
\ Ayuda {{{

: curiosities
  \ Pendiente!!!
  s" Curiosidades..." paragraph  ;

: game$  ( -- a u )
  s{ s" juego!!!" s" programa" }s  ;
: the_game$  ( -- a u )
  s" el" game$ s&  ;
: except$  ( -- a u )
  s{ s" excepto" s" salvo" }s  ;
: way$  ( -- a u )
  s{ s" manera" s" forma" }s  ;
: leave$  ( -- a u )
  s{ s" abandonar" s" detener" s" dejar" s" interrumpir" }s  ;
: left$  ( -- a u )
  s{ s" abandonado" s" detenido" s" dejado" s" interrumpido" }s  ;
: pressing$  ( -- a u )
  s{ s" pulsando" s" mediante" }s  ;
: they_make$  ( -- a u )
  s{ s" forman" s" componen" }s  ;
: instructions_0
  \ Instrucciones sobre el objeto del juego.
  s" El" s{ s" programa" s" juego" }s&
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
  s" cada texto" s&
  s{ s" con" s" usando" s" empleando" s" utilizando" s" incluyendo" }s&
  s{ s" al menos" s" por lo menos" }s?&
  s" un sustantivo relacionado con" s&
  s{ s" el mismo" s" él" }s& comma+
  s" pero que no sea" s&
  s{ s" familia de" s" de la misma familia que" }s&
  s" alguna de las palabras" s&
  s{ 
  s" que lo" they_make$ s& 
  s" que" they_make$ s& s" el texto" s&
  s" que" they_make$ s& s" dicho texto" s&
  s{ s" del" s" de dicho" }s s" texto" s&
  }s& period+
  s" El proceso" s&
  s{ s" se repetirá" s" continuará" s" no acabará" s" no terminará" }s&
  s" hasta que todos los textos hayan sido mostrados y respondidos." s&
  paragraph  ;
: instructions_1
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
  paragraph  ;
: instructions_2
  \ Instrucciones sobre el arranque del juego.
  s" Tanto para empezar a jugar ahora como para hacerlo tras haber"
  left$ s& the_game$ s&
  s" puedes" s& s{ s" usar" s" probar" }s&
  s" cualquier palabra que" s&
  s{ s" se te ocurra" s" que te parezca" }s& comma+ s" hasta" s&
  s{ s" encontrar" s" dar con" s" acertar con" }s&
  s{ s" alguna" s" una" }s& s" que" s&
  s{ s" surta efecto" s" funcione" s" sirva" }s& period+
  paragraph    ;
: instructions  ( -- false )
  \ Inacabado!!!
  page s" Instrucciones de Autohipnosis" paragraph
  instructions_0
  instructions_1
  instructions_2  false  ;

\ }}} ##########################################################
\ Menú {{{

false [if]  \ --------------------------------

\ Borrador descartado de un sistema de menú clásico.

: .menu
  \ Imprime el menú.
  cr ." I...nstrucciones" 
  cr ." J...ugar"
  success? @  if  cr ." C...uriosidades"  then
  cr ." F...in"  ;
: valid_option  ( c1 -- c1 | c2 | false )
  \ ¿Es válida una opción del menú?
  \ c1 = Código de la tecla pulsada
  \ c2 = Código de la tecla válida equivalente
  case
    [char] i  of  [char] i  endof
    [char] j  of  [char] j  endof
    [char] f  of  [char] f  endof
    [char] I  of  [char] i  endof
    [char] J  of  [char] j  endof
    [char] F  of  [char] f  endof
    false swap
  endcase  ;
: menu_option  ( -- c )
  \ Devuelve una opción del menú.
  begin  key valid_option ?dup  until  ;
: menu  ( -- u )
  \ Muestra el menú y espera una opción válida.
  \ Provisional!!!
  .menu menu_option  ;

[then]  \ --------------------------------

: .menu
  \ Imprime el «menú».
  cr s" ¿Qué quieres hacer?" paragraph  ;
: (evaluate_option)
  \ Analiza la fuente actual con los vocabularios activos,
  \ ejecutando las palabras reconocidas.
  begin  parse-name ?dup
  while  find-name ?dup   if  execute_nt  then
  repeat  drop  ;
: evaluate_option  ( a u -- )
  \ Analiza una cadena con el vocabulario del menú. 
  only menu_vocabulary
  ['] (evaluate_option) execute-parsing
  restore_vocabularies  ;
variable finished
: finish
  finished on  ;
: menu  ( -- ff )
  \ Muestra el menú, espera y obedece una opción.
  \ ff = ¿Salir del programa?
  finished off
  .menu command evaluate_option finished @  ;

\ }}} ##########################################################
\ Inicialización {{{

: init/once
  \ Inicialización necesaria antes de la primera partida.
  \ Pendiente!!!
  page  ;
: init/game
  \ Inicialización necesaria antes de cada partida.
  page .centered_title 10 seconds
  init_output_cursor  page  ;

\ }}} ##########################################################
\ Juego {{{

: ask  ( u -- )
  \ Pide al jugador un comando, hasta que recibe uno válido.
  \ u = Número ordinal de la frase actual.
  sentence# !
  begin  command  valid?  until  ;
: step  ( u -- )
  \ Un paso del juego.
  \ u = Número ordinal de la frase actual.
  dup .sentence ask  ;
: game
  \ Bucle de cada partida.
  #sentences @ dup 1
  do  i step  loop
  .sentence  \ Frase final
  the_happy_end  ;
: play
  \ Jugar una partida.
  init/game game   ;

\ }}} ##########################################################
\ Comandos del menú {{{

\ El menú tiene solo tres comandos, cada uno con muchos sinónimos.

also menu_vocabulary definitions

\ Comando «instrucciones»:
' instructions alias ayuda
' instructions alias espera
' instructions alias esperar
' instructions alias espero
' instructions alias ex
' instructions alias examina
' instructions alias examinar
' instructions alias examino
' instructions alias examínate
' instructions alias examínome
' instructions alias i
' instructions alias instrucciones
' instructions alias inventario
' instructions alias lee
' instructions alias leer
' instructions alias leo
' instructions alias m
' instructions alias manual
' instructions alias mira
' instructions alias mirar
' instructions alias miro
' instructions alias mírate
' instructions alias mírome
' instructions alias pista
' instructions alias pistas
' instructions alias registra
' instructions alias registrar
' instructions alias registro
' instructions alias x
\ Comando «jugar»:
' play alias arranca
' play alias arrancar 
' play alias arranco
' play alias comenzar
' play alias comienza
' play alias comienzo
' play alias ejecuta
' play alias ejecutar
' play alias ejecuto
' play alias empezar
' play alias empieza
' play alias empiezo
' play alias inicia
' play alias iniciar 
' play alias inicio
' play alias juego
' play alias jugar
' play alias partida
' play alias probar
' play alias pruebo
\ Comando «fin»:
' finish alias acaba
' finish alias acabar
' finish alias acabo
' finish alias acabose
' finish alias acabó
' finish alias adiós
' finish alias apaga
' finish alias apagar
' finish alias apago
' finish alias apágate
' finish alias cerrar
' finish alias cierra
' finish alias cierre
' finish alias cierro
' finish alias ciérrate
' finish alias concluir
' finish alias conclusión
' finish alias concluye
' finish alias concluyo
' finish alias desconecta
' finish alias desconectar
' finish alias desconecto
' finish alias desconexión
' finish alias desconéctate
' finish alias fin
' finish alias final
' finish alias finaliza
' finish alias finalización
' finish alias finalizar
' finish alias finalizo
' finish alias sal
' finish alias salgo
' finish alias salida
' finish alias salir
' finish alias termina
' finish alias terminación
' finish alias terminar
' finish alias termino
' finish alias término

restore_vocabularies

\ }}} ##########################################################
\ Principal {{{

: main
  \ Bucle principal del juego.
  init/once  begin  menu  until  ;

true  [if]

(

~/forth/autohipnosis/autohipnosis.fs:1236: Invalid memory address
' main alias >>>juego<<<
Backtrace:
$B718C71C @
$B7198774 name>string
$B71988FC (reveal
$FFFFFFFF
$B71989B0 inithash
$B7198C3C addall
$0
$B7198D40 hashdouble
$B719882C hash-alloc
$B7198864 (reveal
$B718FC48 perform
$B718F0F8 reveal

)

' main alias autohipnosis
' main alias arranca
' main alias arrancar 
' main alias arranco
' main alias comenzar
' main alias comienza
' main alias comienzo
' main alias ejecuta
' main alias ejecutar
' main alias ejecuto
' main alias ejecútate
' main alias empezar
' main alias empieza
' main alias empiezo
' main alias inicia
' main alias iniciar 
' main alias inicio
' main alias iníciate
' main alias juega
' main alias juego
' main alias jugar
' main alias partida
' main alias probar
' main alias prueba
' main alias pruebo
' main alias adelante
' main alias ya
' main alias vamos
[else]

(
: juego  main  >>>;<<<
Backtrace:
$B733971C @
$B7345774 name>string
$B73458FC (reveal
$FFFFFFFF
$B73459B0 inithash
$B7345C3C addall
$0
$B7345D40 hashdouble
$B734582C hash-alloc
$B7345864 (reveal
$B733CC48 perform
$B733CA14 reveal

)

: autohipnosis  main  ;
: arranca  main  ;
: arrancar   main  ;
: arranco  main  ;
: comenzar  main  ;
: comienza  main  ;
: comienzo  main  ;
: ejecuta  main  ;
: ejecutar  main  ;
: ejecuto  main  ;
: ejecútate  main  ;
: empezar  main  ;
: empieza  main  ;
: empiezo  main  ;
: inicia  main  ;
: iniciar   main  ;
: inicio  main  ;
: iníciate  main  ;
: juega  main  ;
: juego  main  ;
: jugar  main  ;
: partida  main  ;
: probar  main  ;
: prueba  main  ;
: pruebo  main  ;
: adelante  main  ;
: ya  main  ;
: vamos  main  ;
[then]  main  ;
autohipnosis

\ }}} ##########################################################
\ Notas {{{

false  [if]

Ideas:

Modo de juego por puntos, siempre avanzando, un punto por
acierto.

Pendiente!!!:

Hacer variables los textos de las instrucciones y del presto.

Pedir confirmación de salida, pulsando la barra espaciadora.

[then]

\ }}} ##########################################################
