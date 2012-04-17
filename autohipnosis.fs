\ autohipnosis.fs

\ Fichero principal de:
\ «Autohipnosis»
: version  ( -- a u )  s" A-00-2012033013"  ;  \ versión
\ Juego conversacional experimental

\ Copyright (C) 2012 Marcos Cruz (programandala.net)
\ Licencia/Permesilo/License:
\ http://programandala.net/licencia

\ Este programa está escrito en Forth usando el sistema Gforth:
\ http://www.jwdt.com/~paysan/gforth.html
\ Para escribir este programa se ha empleado el editor Vim:
\ http://www.vim.org

\ Historial de desarrollo:
\ http://programandala.net/es.programa.autohipnosis.historial

\ Información sobre juegos conversacionales:
\ http://caad.es
 
\ }}} ##########################################################
\ Averiguar el sistema Forth {{{

[defined] ficl-set-current DUP
CONSTANT ficl?
CONSTANT [ficl?] IMMEDIATE

S" gforth" ENVIRONMENT? DUP
[IF]  NIP NIP  [THEN]  DUP 
CONSTANT gforth?
CONSTANT [gforth?] IMMEDIATE

\ }}} ##########################################################
\ Herramientas {{{

: show  ( -- )  cr .s  ;
: wait  ( -- )  key drop  ;
: show...  ( -- )  show wait  ;
' true alias [true]  immediate
' false alias [false]  immediate
: ++  ( a -- )
  \ Incrementa el contenido de una dirección de memoria.
  1 swap +!
  ;
: --  ( a -- )
  \ Decrementa el contenido de una dirección de memoria.
  -1 swap +!
  ;
: period+  ( a1 u1 -- a2 u2 )
  \ Añade un punto al final de una cadena.
  s" ." s+
  ;
: comma+  ( a1 u1 -- a2 u2 )
  \ Añade una coma al final de una cadena.
  s" ," s+
  ;

\ }}} ##########################################################
\ Requisitos {{{

\ -----------------------------
\ De «Forth Foundation Library» (versión 0.8.0)
\ (http://code.google.com/p/ffl/)

\ Cadenas de texto dinámicas:
s" ffl/str.fs" included
\ Manejador de secuencias de escape de la consola:
s" ffl/trm.fs" included

\ -----------------------------
\ «csb2», almacén circular de cadenas
\ (http://programandala.net/es.programa.csb2)

s" csb2.fs" included
\ Creamos el almacén circular de cadenas en el diccionario,
\ para que se guarde junto con la imagen del sistema:
\ free_csb  dictionary_csb  2048 allocate_csb

\ }}} ##########################################################
\ Vocabularios {{{

vocabulary game_vocabulary  \ palabras del programa
: restore_vocabularies  ( -- )
  \ Restaura los vocabularios a su orden habitual.
  only forth also game_vocabulary definitions
  ;
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
    case_sensitive_vocabulary player_vocabulary  \ palabras del jugador

[else]

  \ Método alternativo.

  table value (player_vocabulary)
  : player_vocabulary  ( -- )
    \ Reemplaza el vocabulario superior con el del jugador.
    \ Código adaptado de Gforth (compat/vocabulary.fs).
    get-order dup 0= 50 and throw  \ Error 50 («search-order underflow») si la lista está vacía
    nip (player_vocabulary) swap set-order
    ;

[then]

\ vocabulary player_vocabulary  \ palabras del jugador
\ No se usan todavía!!!:
\ vocabulary answer_vocabulary  \ respuestas a preguntas de «sí» o «no»
vocabulary menu_vocabulary  \ palabras para las opciones del menú

\ }}} ##########################################################
\ Pantalla {{{

\ Terminal ANSI

\ Las siguientes palabras están adaptadas de:

\ ansi.4th
\ ANSI Terminal words for kForth
\ Copyright (c) 1999--2004 Krishna Myneni
\ Creative Consulting for Research and Education
\ This software is provided under the terms of the GNU
\ General Public License.

(

Estas palabras no tienen equivalente en el módulo trm de
Forth Foundation Library y por ello se incluyen aquí, aunque
modificadas para que usen palabras del módulo trm y así
evitar duplicidades.

)

: read_cdnumber  ( c -- n )
  \ Lee del terminal un numéro decimal terminado en un carácter.
  >r 0
  begin   key dup r@ <>
  while   swap 10 * swap [char] 0 - +
  repeat  r> 2drop
  ;
: xy  ( -- u1 u2 )
  \ Devuelve la posición actual del cursor.
  \ u1 = columna
  \ u2 = línea
  [char] [ trm+do-esc1 ." 6n"
  key key 2drop  \ Descartar los dos caracteres: <esc> [
  [char] ; read_cdnumber
  [char] R read_cdnumber
  1- swap 1-
  ;
: col  ( -- u )  xy drop  ;
: row  ( -- u )  xy nip  ;
: at-x  ( u -- )  row at-xy  ;
true  [if]
: at-max-xy  ( -- )
  \ Sitúa el cursor en los mayores valores posibles de x e y.
  \ El mayor valor posible es -1 interpretado sin signo;
  \ usamos -2 porque la palabra AT-XY en Gforth incrementa los valores.
  -2 dup at-xy
  ;
: last_row  ( -- u )
  \ Devuelve el número de la última fila de la pantalla.
  trm+save-cursor  at-max-xy row  trm+restore-cursor
  ;
: rows  ( -- u )
  \ Devuelve las filas de la pantalla.
  last_row 1+
  ;
: last_col  ( -- u )
  \ Devuelve el número de la última columna de la pantalla.
  trm+save-cursor  at-max-xy col  trm+restore-cursor
  ;  
: cols  ( -- u )
  \ Devuelve el número de columnas de la pantalla.
  last_col 1+
  ;
[else]
false  [if]
\ Wrong method!!!:
: last_col  ( -- u )  form nip  ;
: last_row  ( -- u )  form drop  ;
: cols  ( -- u )  cols 1+  ;
: rows  ( -- u )  rows 1+  ;
[else]
\ Right method!!!:
\ But the hash error arises!!!
: cols  ( -- u )  form nip  ;
: rows  ( -- u )  form drop  ;
: last_col  ( -- u )  cols 1-  ;
: last_row  ( -- u )  rows 1-  ;
[then]
[then]
\ Debug!!!:
rows . cols . cr
last_row . last_col . cr
wait
: no_window  ( -- )
  \ Desactiva la definición de zona de pantalla como ventana.
  [char] r trm+do-csi0
  ;

: output_window  ( -- )
  \ Selecciona una zona de pantalla para la salida principal
  \ (todas las líneas salvo las dos últimas).
  \ Nótese que TRM+SET-SCROLL-REGION cuenta las líneas empezando por uno,
  \ mientras que ANS Forth cuenta líneas y columnas empezando por cero.
  last_row 1- 1 trm+set-scroll-region
  ;
2variable output-xy  \ Coordenadas del cursor en la ventana de salida
: save_output_cursor  ( -- )
  \ Guarda la posición actual del cursor en la ventana de salida.
  xy output-xy 2!
  ;
: restore_output_cursor  ( -- )
  \ Restaura la posición guardada del cursor en la ventana de salida.
  output-xy 2@ at-xy
  ;
: at_first_output  ( -- )
  \ Sitúa el cursor en la posición en que se ha de imprimir la primera frase
  \ (en la parte inferior de la ventana de salida).
  0 last_row 3 - at-xy
  ;
: init_output_cursor  ( -- )
  output_window
  at_first_output save_output_cursor
  no_window
  ;
: at_input  ( -- )
  \ Sitúa el cursor en la zona de entrada (la última línea).
  0 last_row at-xy
  ;
: input_window  ( -- )
  \ Selecciona una zona de pantalla para la entrada de comandos
  \ (la última línea).
  last_row dup trm+set-scroll-region
  ;
: home  ( -- )  0 dup at-xy  ;

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

: «»-clear  ( -- )
  \ Vacía la cadena dinámica PRINT_STR.
  print_str str-clear
  ;
: «»!  ( a u -- )
  \ Guarda una cadena en la cadena dinámica PRINT_STR.
  print_str str-set
  ;
: «»@  ( -- a u )
  \ Devuelve el contenido de la cadena dinámica PRINT_STR.
  print_str str-get
  ;
: «+  ( a u -- )
  \ Añade una cadena al principio de la cadena dinámica PRINT_STR.
  print_str str-prepend-string
  ;
: »+  ( a u -- )
  \ Añade una cadena al final de la cadena dinámica PRINT_STR.
  print_str str-append-string
  ;
: «c+  ( c -- )
  \ Añade un carácter al principio de la cadena dinámica PRINT_STR.
  print_str str-prepend-char
  ;
: »c+  ( c -- )
  \ Añade un carácter al final de la cadena dinámica PRINT_STR.
  print_str str-append-char
  ;
: «»bl+?  ( u -- ff )
  \ ¿Se debe añadir un espacio al concatenar una cadena a la cadena dinámica PRINT_STR ?
  \ u = Longitud de la cadena que se pretende unir a la cadena dinámica PRINT_STR
  0<> print_str str-length@ 0<> and
  ;
: »&  ( a u -- )
  \ Añade una cadena al final de la cadena dinámica TXT, con un espacio de separación.
  dup «»bl+?  if  bl »c+  then  »+
  ;
: «&  ( a u -- )
  \ Añade una cadena al principio de la cadena dinámica TXT, con un espacio de separación.
  dup «»bl+?  if  bl «c+  then  «+ 
  ;

\ Impresión de párrafos justificados

variable #lines  \ Número de línea del texto que se imprimirá

\ Indentación de la primera línea de cada párrafo (en caracteres):
2 constant default_indentation  \ Predeterminada 
8 constant max_indentation  \ Máxima
variable /indentation  \ En curso

: not_first_line?  ( -- ff )
  \ ¿La línea de pantalla donde se imprimirá es la primera?
  row 0>
  ;
variable indent_first_line_too?  \ ¿Se indentará también la línea superior de la pantalla, si un párrafo empieza en ella?
: indentation?  ( -- ff )
  \ ¿Indentar la línea actual?
  not_first_line? indent_first_line_too? @ or
  ;
: char>string  ( c u -- a u )
  \ Crea una cadena repitiendo un carácter.
  \ c = Carácter
  \ u = Longitud de la cadena
  \ a = Dirección de la cadena
  dup 'csb swap 2dup 2>r  rot fill  2r>
  ;
: indentation+  ( -- )
  \ Añade indentación ficticia (con un carácter distinto del espacio)
  \ a la cadena dinámica PRINT_STR , si la línea del cursor no es la primera.
  indentation?  if
    [char] X /indentation @ char>string «+
  then
  ;
: indentation-  ( a1 u1 -- a2 u2 )
  \ Quita a una cadena tantos caracteres por la izquierda como el valor de la indentación.
  /indentation @ -  swap /indentation @ +  swap
  ;
: indent  ( -- )
  \ Mueve el cursor a la posición requerida por la indentación.
  /indentation @ ?dup  if  trm+move-cursor-right  then
  ;
: indentation>  ( a1 u1 -- a2 u2 ) \ Prepara la indentación de una línea
  indentation?  if  indentation- indent  then
  ;
: .line  ( a u -- )
  \ Imprime una línea de texto y un salto de línea.
  \ type cr
  cr type
  ;
: .lines  ( a1 u1 ... an un n -- )
  \ Imprime n líneas de texto.
  \ a1 u1 = Última línea de texto
  \ an un = Primera línea de texto
  \ n = Número de líneas de texto en la pila
  dup #lines !  
  0  ?do  .line  loop
  ;
: (paragraph)  ( -- )
  \ Imprime la cadena dinámica PRINT_STR ajustándose al ancho de la pantalla.
  indentation+  \ Añade indentación ficticia
  print_str str-get cols str+columns  \ Divide la cadena dinámica PRINT_STR 
  >r indentation> r>  \ Prepara la indentación efectiva de la primera línea
  .lines  \ Imprime las líneas
  print_str str-init  \ Vacía la cadena dinámica
  ;
: paragraph/ ( a u -- )
  \ Imprime una cadena ajustándose al ancho de la pantalla.
  print_str str-set (paragraph)
  ;
: paragraph  ( a u -- )
  \ Imprime una cadena ajustándose al ancho de la pantalla;
  \ y una separación posterior si hace falta.
  \ dup >r  paragraph/ r> ?cr  \ Versión original!!!
  paragraph/ cr
  ;

\ }}} ##########################################################
\ Pausas {{{

: time?  ( d -- ff )  utime d<  ;
: microseconds  ( u -- )
  \ Espera un número de microsegundos o hasta que se pulse una tecla.
  s>d utime d+
  begin  2dup time? key? 0= or  until
  begin  2dup time? key? or  until
  2drop
  ;
: miliseconds  ( u -- )
  \ Espera un número de milisegundos o hasta que se pulse una tecla.
  1000 * microseconds
  ;
: seconds  ( u -- )
  \ Espera un número de segundos o hasta que se pulse una tecla.
  1000000 * microseconds
  ;

\ }}} ##########################################################
\ Herramientas de azar {{{

\ Generador de números aleatorios

s" random.fs" included

: randomize  ( -- )
  \ Reinicia la semilla de generación de números aleatorios.
  time&date 2drop 2drop * seed !
  ;

\ Elegir un elemento al azar de la pila

: drops  ( x1 ... xn n -- )
  \ Elimina n celdas de la pila.
  0  do  drop  loop
  ;
: choose  ( x1 ... xn n -- xn' )
  \ Devuelve un elemento de la pila elegido al azar
  \ entre los n superiores y borra el resto.
  dup >r random pick r> swap >r drops r>
  ;
: dchoose  ( d1 ... dn n -- dn' )
  \ Devuelve un elemento doble de la pila elegido al azar
  \ entre los n superiores y borra el resto.
  dup >r random 2*  ( d1 ... dn n' -- ) ( r: n )
  dup 1+ pick swap 2 + pick swap  ( d1 ... dn dn' -- ) ( r: n )
  r> rot rot 2>r  2* drops  2r>
  ;

\ Elegir una cadena al azar entre varias

(

Para facilitar la selección aleatoria de una cadena entre un
grupo, crearemos las palabras S{ y }S , que proporcionarán
un sintaxis fácil de escribir y crearán un código fácil de
leer. También crearemos variantes que concatenen la
cadena elegida de diversas maneras.

Pero para que las palabras S{ y }S puedan ser anidadas, necesitan una
pila propia en la que guardar la profundidad actual de la pila de Forth
en cada anidación.  Crearemos por ello una pequeña pila en memoria,
con el nombre de «dstack» [por «depth stack»].

)

' dchoose alias schoose  \ Alias de DCHOOSE para usar con cadenas de texto (solo por estética)

4 constant /dstack  \ Elementos de la pila (y por tanto número máximo de anidaciones)
variable dstack>  \ Puntero al elemento superior de la pila (o cero si está vacía)
/dstack cells allot  \ Hacer espacio para la pila
0 dstack> !  \ Pila vacía para empezar
: 'dstack>  ( -- a )
  \ Dirección del elemento superior de la pila.
  dstack> dup @ cells + 
  ;
: dstack_full?  ( -- ff )
  \ ¿Está la pila llena?
  dstack> @ /dstack =
  ;
: dstack_empty?  ( -- ff )
  \ ¿Está la pila vacía?
  dstack> @ 0=
  ;
: dstack!  ( u -- )
  \ Guarda un elemento en la pila.
  dstack_full? abort" Error de anidación de S{ y }S : su pila está llena."
  dstack> ++ 'dstack> ! 
  ;
: dstack@  ( -- u )
  \ Devuelve el elemento superior de la pila.
  dstack_empty? abort" Error de anidación de S{ y }S : su pila está vacía."
  'dstack> @ dstack> --
  ;
: s{  ( -- )
  \ Inicia una zona de selección aleatoria de cadenas.
  depth dstack!
  ;
: }s  ( a1 u1 ... an un -- a' u' )
  \ Elige una cadena entre las puestas en la pila
  \ desde que se ejecutó por última vez la palabra S{ .
  depth dstack@ - 2 / schoose
  ;
: }s&  ( a0 u0 a1 u1 ... an un -- a' u' )
  \ Elige una cadena entre las puestas en la pila desde que se ejecutó S{
  \ y la concatena (con separación) a una cadena anterior.
  }s s&
  ;
: }s+  ( a0 u0 a1 u1 ... an un -- a' u' )
  \ Elige una cadena entre las puestas en la pila desde que se ejecutó S{
  \ y la concatena a una cadena anterior.
  }s s+
  ;
: s?  ( a u -- a u | a 0 )
  \ Vacía una cadena (con el 50% de probabilidad).
  2 random *
  ;
: s?&  ( a1 u1 a2 u3 -- a3 u3 )
  \ Devuelve una cadena concatenada o no (al azar) a otra,
  \ con separación.
  s? s&
  ;
: s?+  ( a1 u1 a2 u3 -- a3 u3 )
  \ Devuelve una cadena concatenada o no (al azar) a otra.
  s? s+
  ;
: s+?  ( a1 u1 a2 u3 -- a3 u3 | a3 0 )
  \ Devuelve dos cadenas concatenadas
  \ o (al azar) una cadena vacía.
  s+ s?
  ;
: s&?  ( a1 u1 a2 u3 -- a3 u3 | a3 0 )
  \ Devuelve dos cadenas concatenadas (con separación)
  \ o (al azar) una cadena vacía.
  s& s?
  ;
: }s?  ( a1 u1 ... an un -- a' u' | a' 0 )
  \ Elige una cadena entre las puestas en la pila desde que se ejecutó S{
  \ y la vacía con el 50% de probabilidad.
  }s s?
  ;
: }s?&  ( a0 u0 a1 u1 ... an un -- a' u' )
  \ Elige una cadena entre las puestas en la pila desde que se ejecutó S{
  \ y (con un 50% de probabilidad) la concatena (con separación)
  \ a una cadena anterior.
  }s? s&
  ;
: }s?+  ( a0 u0 a1 u1 ... an un -- a' u' )
  \ Elige una cadena entre las puestas en la pila desde que se ejecutó S{
  \ y (con un 50% de probabilidad) la concatena (sin separación)
  \ a una cadena anterior.
  }s? s+
  ;
: s&{  ( a1 u1 a2 u2 -- a3 u3 )
  \ Concatena dos cadenas (con separación)
  \ e inicia una zona de selección aleatoria de cadenas.
  s& s{
  ;
: s+{  ( a1 u1 a2 u2 -- a3 u3 )
  \ Concatena dos cadenas (sin separación)
  \ e inicia una zona de selección aleatoria de cadenas.
  s+ s{
  ;

\ Combinar cadenas de forma aleatoria

: rnd2swap  ( a1 u1 a2 u2 -- a1 u1 a2 u2 | a2 u2 a1 u1 )
  \ Intercambia (con 50% de probabililad) la posición de dos textos.
  2 random  if  2swap  then
  ;
: (both)  ( a1 u1 a2 u2 -- a1 u1 a3 u3 a2 u2 | a2 u2 a3 u3 a1 u1 )
  \ Devuelve las dos cadenas recibidas, en cualquier orden,
  \ y separadas en la pila por la cadena «y».
  rnd2swap s" y" 2swap
  ;
: both  ( a1 u1 a2 u2 -- a3 u3 )
  \ Devuelve dos cadenas unidas en cualquier orden por «y».
  \ Ejemplo: si los parámetros fueran «espesa» y «fría»,
  \ los dos resultados posibles serían: «fría y espesa» y «espesa y fría».
  (both) s& s&
  ;
: both&  ( a0 u0 a1 u1 a2 u2 -- a3 u3 )
  \ Devuelve dos cadenas unidas en cualquier orden por «y»; y concatenada (con separación) a una tercera.
  both s&
  ;
: both?  ( a1 u1 a2 u2 -- a3 u3 )
  \ Devuelve al azar una de dos cadenas,
  \ o bien ambas unidas en cualquier orden por «y».
  \ Ejemplo: si los parámetros fueran «espesa» y «fría»,
  \ los cuatro resultados posibles serían:
  \ «espesa», «fría», «fría y espesa» y «espesa y fría».
  (both) s&? s&
  ;
: both?&  ( a0 u0 a1 u1 a2 u2 -- a3 u3 )
  \ Concatena (con separación) al azar una de dos cadenas
  \ (o bien ambas unidas en cualquier orden por «y») a una tercera cadena.
  both? s&
  ;
: both?+  ( a0 u0 a1 u1 a2 u2 -- a3 u3 )
  \ Concatena (sin separación) al azar una de dos cadenas
  \ (o bien ambas unidas en cualquier orden por «y») a una tercera cadena.
  both? s+
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
  col
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
  output_window restore_output_cursor
  sentence$ paragraph/
  save_output_cursor  no_window
  ;

: hs,  ( a u -- a1 )
  \ Compila una cadena en el diccionario
  \ y devuelve su dirección.
  here rot rot s,
  ;
: sentence:  ( a u "name" -- a1 )
  \ Compila una frase, devuelve su dirección
  \ y crea una constante que devolverá su número ordinal.
  hs,  #sentences 1 over +!  @ constant
  ;

\ Crear las frases, definidas en un fichero independiente:
s" autohipnosis_sentences.fs" included

\ En este punto, las direcciones de todas las frases
\ están en la pila y la variable SENTENCES# contiene
\ el número de frases que han sido creadas.
( a1 ... an )

: sentences,  ( a1 ... an -- )
  \ Compila las direcciones de las frases.
  #sentences @ 0  do  ,  loop
  ;

\ Tabla para las direcciones de las frases
create ('sentences)  ' ('sentences) is 'sentences
sentences,  \ Rellenar la tabla compilando su contenido

: associated?  ( u a | a u -- ff )
  \ ¿Está una palabra del juego asociada a una frase?
  \ u = Identificador de la frase
  \ a = Dirección de la zona de datos de la palabra
  + c@ 0<>
  ;
: associate  ( u a -- )
  \ Asocia una frase a una palabra del juego.
  \ u = Identificador de la frase
  \ a = Dirección de la zona de datos de la palabra
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
: create_term_array  ( -- )
  \ Crea la matriz de datos de una palabra del juego.
  \ Cada palabra del juego tiene una matriz para marcar
  \ las frases con las que está asociada.
  \ Para simplificar el código, se usa una matriz de octetos
  \ en lugar de una matriz de bitios: tantos octetos
  \ como frases hayan sido definidas.
  here  #sentences @ dup allot align  erase
  ;
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
  repeat
  ;
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
  restore_vocabularies
  ;

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
  sentence# @ associated? abs valid +!
  ;
: execute_term  ( nt -- )
  \ Executa un término asociado a una frase.
  \ nt = Identificador de nombre del término
  execute_nt  ( a ) valid++
  ;
: (evaluate_command)  ( -- )
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
: /command  ( -- u )
  \ Longitud máxima de un comando
  cols
  ;
: init_command_line  ( -- )
  \ Limpia la línea de comandos y sitúa el cursor.
  0 last_row at-xy trm+erase-line
  ;
create 'command /command chars allot align
: command  ( -- a u )
  \ Acepta un comando del jugador.
  init_command_line
  'command /command accept  'command swap
  no_window
  ;

\ }}} ##########################################################
\ Final {{{

variable success?  \ ¿Se ha completado con éxito el juego?

: the_happy_end  ( -- )
  \ Pendiente!!!
  success? on
  ;

\ }}} ##########################################################
\ Ayuda {{{

: curiosities  ( -- )
  \ Pendiente!!!
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
  s" Tu" s{ s" objetivo" s" misión" }s
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
  paragraph
  ;
: instructions_2  ( -- )
  \ Instrucciones sobre el arranque del juego.
  s" Tanto para empezar a jugar ahora como para hacerlo tras haber"
  left$ s& the_game$ s&
  s" puedes" s& s{ s" usar" s" probar" }s&
  s" cualquier palabra que" s&
  s{ s" se te ocurra" s" que te parezca" }s& comma+ s" hasta" s&
  s{ s" encontrar" s" dar con" s" acertar con" }s&
  s{ s" alguna" s" una" }s& s" que" s&
  s{ s" surta efecto" s" funcione" s" sirva" }s& period+
  paragraph  
  ;
: instructions  ( -- false )
  \ Inacabado!!!
  page s" Instrucciones de Autohipnosis" paragraph
  instructions_0
  instructions_1
  instructions_2  false
  ;

\ }}} ##########################################################
\ Menú {{{

false [if]  \ --------------------------------

\ Borrador descartado de un sistema de menú clásico.

: .menu  ( -- )
  \ Imprime el menú.
  cr ." I...nstrucciones" 
  cr ." J...ugar"
  success? @  if  cr ." C...uriosidades"  then
  cr ." F...in"
  ;
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
  endcase
  ;
: menu_option  ( -- c )
  \ Devuelve una opción del menú.
  begin  key valid_option ?dup  until
  ;
: menu  ( -- u )
  \ Muestra el menú y espera una opción válida.
  \ Provisional!!!
  .menu menu_option
  ;

[then]  \ --------------------------------

: .menu  ( -- )
  \ Imprime el «menú».
  cr s" ¿Qué quieres hacer?" paragraph
  ;
: (evaluate_option)  ( -- )
  \ Analiza la fuente actual con los vocabularios activos,
  \ ejecutando las palabras reconocidas.
  begin  parse-name ?dup
  while  find-name ?dup   if  execute_nt  then
  repeat  drop
  ;
: evaluate_option  ( a u -- )
  \ Analiza una cadena con el vocabulario del menú. 
  only menu_vocabulary
  ['] (evaluate_option) execute-parsing
  restore_vocabularies
  ;
variable finish?
: finish  ( -- )
  finish? on
  ;
: menu  ( -- ff )
  \ Muestra el menú, espera y obedece una opción.
  \ ff = ¿Salir del programa?
  finish? off
  .menu command evaluate_option finish? @
  ;

\ }}} ##########################################################
\ Inicialización {{{

: init/once  ( -- )
  \ Inicialización necesaria antes de la primera partida.
  \ Pendiente!!!
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
  \ Jugar una partida
  init/game game 
  ;

\ }}} ##########################################################
\ Comandos del menú {{{

\ El menú tiene solo tres comandos, cada uno con muchos sinónimos.

also menu_vocabulary definitions

\ Comando «instrucciones»:
' instructions alias ayuda
' instructions alias ex
' instructions alias examina
' instructions alias examinar
' instructions alias examínate
' instructions alias examino
' instructions alias examínome
' instructions alias i
' instructions alias instrucciones
' instructions alias inventario
' instructions alias leer
' instructions alias lee
' instructions alias leo
' instructions alias m
' instructions alias manual
' instructions alias mira
' instructions alias mirar
' instructions alias mírate
' instructions alias miro
' instructions alias mírome
' instructions alias pista
' instructions alias pistas
' instructions alias registra
' instructions alias registrar
' instructions alias registro
' instructions alias x
' instructions alias espera
' instructions alias esperar
' instructions alias espero
' instructions alias z
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
' finish alias desconecta
' finish alias desconectar
' finish alias desconecto
' finish alias desconéctate
' finish alias concluir
' finish alias conclusión
' finish alias concluye
' finish alias concluyo
' finish alias finaliza
' finish alias finalización
' finish alias finalizar
' finish alias finalizo
' finish alias fin
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

: main  ( -- )
  \ Bucle principal del juego.
  init/once  begin  menu  until
  ;

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
