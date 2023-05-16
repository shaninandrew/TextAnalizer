// See https://aka.ms/new-console-template for more information
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Text;

Console.WriteLine("Поиск различий в строках и файлах");

DiffStrings compare = new DiffStrings("кузя сел в лужу", " лужу");
compare.ShowDifference();

DiffStrings compare2 = new DiffStrings("кузя сел в лужу", "кузя лужу");
compare2.ShowDifference();

DiffStrings compare3 = new DiffStrings("кузя сел в лужу", "пруд");
compare3.ShowDifference();

DiffStrings compare4 = new DiffStrings("Николай Петрович хотел выпить", "Николай Васильевич хотел поесть");
compare4.ShowDifference();

DiffStrings compare5 = new DiffStrings("Николай Петрович хотел выпить", " Николай Васильевич  хотел веселиться");
compare5.ShowDifference();


DiffStrings compare6 = new DiffStrings(" Николай Петрович хотел выпить", "Николай Петрович хотел есть");
compare6.ShowDifference();

DiffStrings compare7 = new DiffStrings(" Николай Петрович хотел выпить.  Николай Петрович хотел веселиться! Николай Петрович хотел есть", "Николай Петрович хотел есть");
compare7.ShowDifference();



Console.WriteLine();

Console.Write("Перефраз: Основное -> ");
List<Frag> selectRepeatings = compare7.chain.Where(item => item.type_of_frag == Types_of_Frag.same && item.repeatings > 0).ToList<Frag>();

foreach (Frag f in  selectRepeatings)
        foreach (Frag t in f.duplicates.AsParallel())
            {
                //Выводим только дубли, но без их повторов Иванович Иванович и т.п.
                 if (f.index < t.index)
                    { Console.Write($"{f.GetFrag()} "); }
            }

Console.Write(" + отличия: ");
foreach (Frag f in compare7.chain.Where(item => item.type_of_frag == Types_of_Frag.different))
    Console.Write($"{f.GetFrag()}");
    

Console.WriteLine();

/// <summary>
/// Структура фаргментов
/// </summary>
/// 

enum Types_of_Frag :int
{
  different =0,
  same =1,
  service=2

}
class Frag
{
    string _frag = "";
    long _position = 0;
    long _index = 0; //порядковый номер в цепи

    Types_of_Frag _type = Types_of_Frag.different;

    public long repeatings = 0;

    public Frag(string frag, long position, Types_of_Frag type, long index=0)
    {
        _frag = frag;
        _position = position;
        _type = type;
        _index = index;
     
    }

    public string GetFrag() {  return _frag; }
    public long GetPos    () { return _position; }

    public void Set(string frag, long position)
    {
        _frag = frag;
        _position = position;
    }

    public bool Equals(string v)
    {
        return (_frag.Equals(v));
    }

    //объединяет текущий фрагмент и другой если они стыкуются
    public Frag? Merge(Frag other)
    {
        //если рядом
        if (this._position + this.lenght == other._position)
        { return (new Frag(this._frag + other._frag, this._position, this._type,this._index)); }
        else //если 2 часть 1-ого
        if ((this._position + this.lenght > other._position) && (this._position < other._position))
        {
            //склеиваем разницу
            // abcdef
            //   cdefhgh
            // abcdefhgh
            // , (int) (other._position - this._position
            long tail_len = 1;
            //tail_len =  other.position - this.position ;
            /*
            if ((this.position - other.position) == 0)
            {
                tail_len = this.lenght - other.lenght;

            }
            else
            {
                tail_len = this.lenght - other.lenght;
                if (tail_len < 1) { tail_len = 1;  }
            }
            */
            string new_value = this._frag + other._frag.Substring(  (int)(other.lenght - tail_len  ));
            return (new Frag(new_value, this._position,_type,this._index));

        }

        else { return null; }
    
    }

    /// <summary>
    /// Длина фрагмента
    /// </summary>
    public long lenght { get {return ( _frag.Length); } }
    //Позиция в строке
    public long position { get { return _position; } }
    /// <summary>
    /// Индекс по-порядку 0...N
    /// </summary>
    public long index  { get { return _index; } set { _index = value; } }
    
    /// <summary>
    /// Тип фрагмента
    /// </summary>
    public Types_of_Frag type_of_frag { get { return _type; }  }

    /// <summary>
    /// Идентичные =другим фрагментам (дубли)
    /// </summary>
    public List<Frag> duplicates = new List<Frag> ();

}
class DiffStrings
{
    List<Frag> same_frags = new List<Frag>();
    List<Frag> different_frags = new List<Frag>();
    
    //полная цепочка по порядку
    public List<Frag> chain = new List<Frag>();
    
    //полный словарь всех фрагментов
    List<Frag> journal  = new List<Frag>();
    long Max_len_frag = 1;

    //Исходные данные
    string _a = "";
    string _b = "";



    /// <summary>
    /// Поиск сходства и различия строк
    /// </summary>
    /// <param name="a">Проверяемая строка на сходство</param>
    /// <param name="b">Еще одна проверяемая строка на сходство</param>
    public DiffStrings(string a, string b)
    {

        _a = a;
        _b = b;

        // Лист фрагментов которые надо искать
        List <string> what_find = new List<string> ();

        //будем искать то что менее длины        
        string where_find = _b;

        if (b.Length < a.Length)
        {
            what_find.Add(_b);
            where_find = _a;
        }
        else
        {
            what_find.Add(_a);
        }

        //длина куска текста
        long len = what_find[0].Length ;
        string data_search = what_find[0];

        //очистка журнала
        journal.Clear();

        //готовим словарь
        for (long i =  len; i>0; i--)
        {
            //длина фрагмента
            long frag_len =  i ;
            //кол-во фрагментов в исследуемой строке
            long count_frags = where_find.Length / frag_len+1;

            for (long j_exterior = 0; j_exterior <= count_frags; j_exterior++)
            {
                //дергаем кусок текста длиной  из искомого текста
                try
                {
                   // Thread thread = new Thread(() =>
                   // {
                    string data = data_search+"";
                    long j = j_exterior;

                    //остаток
                    int tmp_frag_len = (int) (data.Length - (j * frag_len));
                    //if (tmp_frag_len < 0) return;
                    if (tmp_frag_len < 0) break;

                    // не надо все копировать, только порцию
                    if (tmp_frag_len > frag_len)
                        { tmp_frag_len = (int) frag_len; }

                    
                        for (int mode = 0; mode < 4; mode++)
                            for (int z = 0; z < tmp_frag_len; z++)
                            {
                                //делаем сужающийся набор для режим 0
                                /// abracadabra
                                ///  bracadar
                                ///   racada
                                ///    acad
                                string tmp = "";
                                if (mode == 0)
                                    tmp = data.Substring((int)(j * frag_len + z), tmp_frag_len - z);

                                /// режим 1
                                ///  abcdefgh
                                ///  abc
                                ///  bcd
                                ///  cde
                                ///  ...
                                if (mode == 1)
                                    tmp = data.Substring((int)(j * frag_len + z), tmp_frag_len);

                                if (mode == 2)
                                    tmp = data.Substring((int)(j * frag_len ), tmp_frag_len-z);
                            
                                if (mode == 3)
                                tmp = data.Substring((int)(j * frag_len-z), tmp_frag_len + z);


                            if (tmp.Length >0)
                                {
                                    Frag new_frag = new Frag(tmp, j * frag_len + z, Types_of_Frag.service);

                                    //бинарная сортировка
                                    if (journal.Count > 0)
                                    {
                                        // если элемент длиннее чем 1, то ставим в начало
                                        if (journal.First<Frag>().lenght < new_frag.lenght)
                                        {
                                            journal.Insert(0, new_frag);
                                        }
                                        else
                                        // если короче последнего, то в конец
                                        if (journal.Last<Frag>().lenght > new_frag.lenght)
                                        {
                                            journal.Add(new_frag);
                                        }
                                        else
                                        //если короче середнего то чуть вниз
                                        if (journal[journal.Count / 2].lenght > new_frag.lenght)
                                        {
                                            journal.Insert(journal.Count / 2 + 1, new_frag);
                                        }
                                        else //вверх
                                        if (journal[journal.Count / 2].lenght < new_frag.lenght)
                                        {
                                            journal.Insert(journal.Count / 2 - 1, new_frag);
                                        }
                                        else //иначе, просто добавляем
                                        {
                                            journal.Add(new_frag);
                                        }
                                    }
                                    else
                                    {
                                        journal.Add(new_frag);
                                    }
                                } // if len>2

                                //статистика самого длинного фрага
                                if (Max_len_frag < tmp.Length) Max_len_frag = (long)tmp.Length;
                            }//for

                  //  });
                  //  thread.Start();


                }
                catch (Exception ex)
                { 
                    Debug.WriteLine(ex.ToString()); 
                 //пропускаем, что нельзя скопировать
                }
            } //for j
        } //for  i

        //Попробуем найти малое в большом
        //   цукер в цукерберге -> цукер:0 ; берге:6
        // будем искать большие куски, если их нет - будем дробить по 1/2
        // пробуем еще разбить так 1 нахдим i=1...n символ - если нашли, то 2..N т.д.

       long i_where =0;
       long  end_where=where_find.Length;
        //всегда пустой элемент в начале
        // different_frags.Add(new Frag("", 0));
        
        //Выстраим цепочку
        long index_chain = 0;
        do
        {
            //не нашли
            bool ok = false;
            long tmp_len = 0;

            foreach (Frag f in journal.AsParallel())
            {
                ok = false;
                //режем кусок строки с позиции - длиной из нашего журнала фрагментов
                tmp_len = f.lenght;
                if ((end_where - (i_where + f.lenght)) < 0)
                {
                    tmp_len = end_where - i_where; //чтобы не копировать фрагменты длинее хвоста и не вызывать ошибок
                }

                //дошли до конца
                if (tmp_len <2) { break; }

                string tmp = where_find.Substring((int) i_where, (int)tmp_len );
                //если 1 в 1
                ok = f.Equals(tmp);
               
                // Отладка
               // Console.WriteLine($" * {tmp} ? {f.GetFrag()} - - {i_where}");


                if (ok)
                {
                   // Console.WriteLine($" {tmp} - {i_where}");
                    Frag checked_frag = new Frag(tmp, i_where,Types_of_Frag.same, index_chain);
                    index_chain++;
                    //нашли похожий фрагмент - идем далее
                    same_frags.Add ( checked_frag);
                
                    //смещаемся на длину фрагмента
                    i_where = checked_frag.position + checked_frag.lenght;
                    break;
                }

            }//for each

            //убираем дубли
            journal= journal.Distinct().AsParallel().ToList<Frag>();

            //ходим по тексту
            Frag last2 = null;

            if (!ok)
            {
                if (different_frags.Count == 0)
                {
                    different_frags.Add(new Frag("", i_where, Types_of_Frag.different, index_chain));
                    index_chain++;
                }


                last2 = different_frags.Last<Frag>();
                long start_text_pos = 0;
                long end_text_pos = i_where;

                if (last2.position == 0)
                {
                    start_text_pos = 0;
                    end_text_pos = i_where;
                }

                if (same_frags.Count > 0)
                {
                    // тут ошибка: возможно A1
                    // убрано +1 - приводит к съеданию символов
                    start_text_pos = same_frags.Last().position + same_frags.Last().lenght  ;//+1
                    end_text_pos = i_where;
                }

                // тут ошибка: возможно
                long size = (end_text_pos - start_text_pos+1); //+1
                if (size < 1) { 
                    size = 1; 
                }


                //для отладки
                //int t = where_find.Length;
                try
                {
                    long index_tmp = different_frags[different_frags.Count - 1].index;

                    Frag fx = new Frag(where_find.Substring((int)start_text_pos, (int)size), start_text_pos,Types_of_Frag.different,index_tmp);
                    different_frags[different_frags.Count - 1] = fx;
                }
                catch { }

            } //not ok


            if (ok)
            {
                last2 = same_frags.Last<Frag>();

                //Добавляем следующий фрагмент
                //  тут ошибка: возможно +1  - убрано
                // A2 
                int start =(int) (same_frags.Last().position + same_frags.Last().lenght ); 
                try
                {
                    Frag fx = new Frag(where_find.Substring(start, 1), start, Types_of_Frag.different);
                    different_frags.Add(fx);
                }
                catch { }

            }

            //позиция после фрагмента с защитой от ошибок
            
            long last_i_where = i_where;
            // A3 +1 проверим
            i_where = last2.position + last2.lenght +1; //+1 OK
            if (last_i_where > i_where) { i_where = last_i_where + 1; }


        }
        while (i_where <= end_where); //скорость выше

        //Строим цепь правильной последовательности
        chain = new List<Frag>();
        //позиция в тексте
        i_where = 0;
        //индекс по массиву
        int i_same = 0;
        int i_diff = 0;
        
        //индекс в цепи
        index_chain = 0;
        chain.Clear();

        //клеим 2 массива
        for (int i = 0; i < (same_frags.Count + different_frags.Count)*2; i++) 
        {

            if (i_same < same_frags.Count)
            {
                if (same_frags[i_same].position == i_where)
                {
                    Frag tmp = same_frags[i_same];
                    tmp.index = index_chain;
                    index_chain++;
                    chain.Add(tmp);
                    i_where = tmp.position + tmp.lenght;
                    i_same++;
                }
                
            }

        
            if (i_diff < different_frags.Count)
            {

                if (different_frags[i_diff].position == i_where)
                {
                    Frag tmp = different_frags[i_diff];
                    tmp.index = index_chain;
                    index_chain++;
                    //tmp.type_of_frag = Types_of_Frag.different;
                    chain.Add(tmp);
                    i_where = tmp.position + tmp.lenght;
                    i_diff++;
                }
                
            }
         

        } //for

        /// ---- Чистим память-----
        same_frags.Clear();
        different_frags.Clear();
        journal.Clear();
        GC.Collect();

        /// Расчет статистики - повторы в параллельном режиме
        foreach (Frag f in chain.AsParallel())
        { 
            
         foreach (Frag tmp in chain.AsParallel())
            {
                if (f.index !=tmp.index)
                {
                    if (f.GetFrag().Equals(tmp.GetFrag()))
                    {
                        f.repeatings++;

                        //ссылочный массив дубликатов
                        f.duplicates.Add(tmp);
                    }
                }
            }
        
        }


    }//DiffString




    public void ShowDifference()
    {
        Console.OutputEncoding = Encoding.Unicode;

        Console.WriteLine("Сравнивалось: ");
        Console.WriteLine($"«{_a}» <--> «{_b}» ");

        ConsoleColor save = Console.BackgroundColor;

        foreach (Frag frag in chain.AsParallel())

        {
            if (frag.type_of_frag == Types_of_Frag.same)
            {
                Console.BackgroundColor = ConsoleColor.Green;
            }

            if (frag.type_of_frag == Types_of_Frag.different)
            {
                Console.BackgroundColor = ConsoleColor.Red;
            }

            Console.Write($"{frag.GetFrag()}");
            Console.BackgroundColor = save;
            Console.Write($" ");

        }

        Console.WriteLine("");

        
        //
        /* 
         * Console.WriteLine("Журнал ___________________________");
         foreach (Frag f in journal)
         { 
             Console.WriteLine($"  {f.GetFrag()}  {f.GetPos()}");
         }
        */
      
        /*
         * Console.WriteLine("Одинаковые фрагменты _____________");
        foreach (Frag f in same_frags)
        {
            Console.WriteLine($"  [{f.GetFrag()}] -  {f.GetPos()} {f.lenght}");
        }

        Console.WriteLine("Разные фрагменты__________________");
        foreach (Frag f in different_frags)
        {
            Console.WriteLine($"  [{f.GetFrag()}] - {f.GetPos()} {f.lenght}");
        }
        */

    }

}//class


