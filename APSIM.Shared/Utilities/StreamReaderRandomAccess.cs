using System.IO;
using System.Text;
using System;
using System.Globalization;

namespace APSIM.Shared.Utilities
{
    [Serializable]
    class StreamReaderRandomAccess
    {
        const int BUFFER_SIZE = 1024;


        private StreamReader g_file = null;
        private int g_position = 0;
        private char[] g_buffer = new char[BUFFER_SIZE + 1];
        private int g_bufferSize = 0;
        private int g_offset = 0;
        private bool g_eofFlag = true;
        private StringBuilder g_lineBuffer = new StringBuilder(BUFFER_SIZE);
        private int g_bufferOffset = 0;

        public StreamReaderRandomAccess(string filename)
        {
            Open(filename);
        }

        public StreamReaderRandomAccess(Stream stream)
        {
            Open(stream);
        }

        private void Open(string filename)
        {

            if ((g_file != null)) Close();

            g_file = new StreamReader(filename);
            g_position = 0;
            g_eofFlag = false;
            g_bufferSize = 0;
            g_bufferOffset = 0;

            LoadBuffer();

        }

        private void Open(Stream stream)
        {

            if ((g_file != null)) Close();

            g_file = new StreamReader(stream);
            g_position = 0;
            g_eofFlag = false;
            g_bufferSize = 0;
            g_bufferOffset = 0;

            LoadBuffer();

        }

        public bool Close()
        {

            g_file.Close();
            g_file = null;
            g_position = 0;
            g_eofFlag = true;
            g_bufferSize = 0;

            return true;

        }

        public int Position
        {
            get { return g_offset; }
            set
            {
                Seek(value, SeekOrigin.Begin);
                g_eofFlag = false;
            }
        }
        public void Seek(int Offset, System.IO.SeekOrigin Origin)
        {
            g_eofFlag = false;
            g_offset = (int)g_file.BaseStream.Seek(Offset, Origin);

            g_file.DiscardBufferedData();

            LoadBuffer();
        }

        public string ReadLine()
        {
            if (EndOfStream)
                return "";
            g_lineBuffer.Length = 0;

            char ch = '\0';
            bool flag = false;

            while (!flag)
            {

                ch = g_buffer[g_position];

                if (ch == '\r')
                { }   // do nothing - skip cr

                else if (ch == '\n')
                {
                    flag = true;
                }
                else
                {
                    g_lineBuffer.Append(ch);
                }

                g_position = g_position + 1;

                if (g_position == g_bufferSize)
                {
                    if (!LoadBuffer())
                    {
                        break; // TODO: might not be correct. Was : Exit While
                    }
                }
            }


            if (flag)
            {
                g_offset = g_bufferOffset + g_position;
                return g_lineBuffer.ToString();
            }

            return "";
        }

        private bool LoadBuffer()
        {

            g_bufferOffset = Convert.ToInt32(g_file.BaseStream.Position, CultureInfo.InvariantCulture);
            g_position = 0;
            g_bufferSize = g_file.Read(g_buffer, 0, BUFFER_SIZE);

            if (g_bufferSize == 0)
            {
                g_eofFlag = true;
                return false;
            }

            return true;

        }

        public bool EndOfStream
        {
            get
            {
                return g_eofFlag;
            }
        }
    }
}