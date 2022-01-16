namespace FlowProtocol.Helper
{
    public class ObjectArray<T>
    {
        public T[,]? Array { get; set; }
        public int Rows {get; private set;} 
        public int Cols {get; private set;}

        public ObjectArray()
        {
            Rows = 0;
            Cols = 0;
        }

        public void ReadList(List<T> liste, int cols = -1)
        {
            int count = liste.Count;
            if (cols == -1)
            {
                if (count > 10) cols = 4;
                else if(count > 4) cols = 3;
                else if (count > 1) cols = 2;
                else cols = 1;
            }
            Cols = cols;
            if (Cols <= 0) Cols = 1;
            Rows = count / Cols;
            if (count % Cols > 0) Rows++;
            Array = new T[Rows,Cols];
            int idxRow=0;
            int idxCol=0;
            foreach(T idx in liste)
            {
                Array[idxRow,idxCol] = idx;
                idxCol++;
                if (idxCol>=Cols)
                {
                    idxRow++;
                    idxCol = 0;
                }
            }         
        }
    }
}