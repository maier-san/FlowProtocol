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

        public void ReadList(List<T> liste, int cols)
        {
            int count = liste.Count;
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