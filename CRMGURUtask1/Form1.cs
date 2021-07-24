using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace CRMGURUtask1
{
    public partial class Form1 : Form
    {
        
        private SqlConnection _sqlConnection = null;

        private (string, bool) _response=(string.Empty,true);
        
        private string _check1 = string.Empty;
        private string _check2 = string.Empty;
        private string _check3 = string.Empty;// она  не нужна, добавлена, чтобы код не дублировать для table Country

        private List<UserResponse> userResponse;
        

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try 
            {
                _sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["CRMGURUDB"].ConnectionString); 
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Не удалось создать строку подключения {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                
                SqlDataAdapter dataAdapter = new SqlDataAdapter
                    (textBox1.Text, _sqlConnection);
                DataSet dataSet = new DataSet();
                dataAdapter.Fill(dataSet);
                dataGridView1.DataSource = dataSet.Tables[0];

            }
            catch (DataException ex)
            {
                MessageBox.Show($"{ex.Message}");
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"SQL запрос провален. {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неправильный запрос {ex.Message}");
            }
            
        }


        private void button2_Click(object sender, EventArgs e)
        {
            _response = InputCountry();

            if (_response.Item2 == false)
                return;

            try
            {
                userResponse = JsonConvert.DeserializeObject<List<UserResponse>>(_response.Item1);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Запрос не записался в переменную. {ex.Message}");
                return;
            }

            if (userResponse.Count >= 2)//Чтобы в запросе не было больше двух. Например если ввести AU то выведется и Australia, и Austria
            {
                MessageBox.Show("Попробуйте заново. Под ваш запрос попадает больше 1 страны");
            }
            else
            {
                dataGridView2.AutoGenerateColumns = true;
                dataGridView2.DataSource = userResponse;

               
                string caption = "Сохранение данных";
                string message = "Вы хотите сохранить данные в базу данных?";
                DialogResult result = MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    FillDataInDB();
                }
                else
                {
                    return;
                }
               
            }

            userResponse = null;
          


        }

        private void FillDataInDB()
        {
            //CITY
            _check1 = CheckDB( "Select name from city where Name=", "Select id,name from city where Name=", "insert into city (name) values", userResponse[0].Capital,false);
            //REGION
            _check2 = CheckDB("Select name from region where Name=", "Select id,name from region where Name=", "insert into region (name) values", userResponse[0].Region,false);
            //country
            _check3 = CheckDB("Select code from country where code=", "Select id,code from country where code=", "insert into country(name, code, capital, area, Population, region) values", userResponse[0].Alpha3Code,true);
            
            MessageBox.Show("Данные сохранены");
        }

        private string CheckDB(string selectDataAdapter, string selectQuery, string insert, string value, bool trigger)
        {
            string check = string.Empty;
        
            SqlDataAdapter da = new SqlDataAdapter($"{selectDataAdapter}'{value}'", _sqlConnection);
            DataTable dt = new DataTable();
            da.Fill(dt);

            _sqlConnection.Open();
            SqlCommand sqlCommand;

            if (dt.Rows.Count >= 1)
            {
                if (trigger == true)// этот триггер нужен для третьего запроса, у которого есть update
                {
                    sqlCommand = new SqlCommand($"update country Set name = '{userResponse[0].Name}',    code = '{userResponse[0].Alpha3Code}',    Capital = {_check1},    area = {userResponse[0].Area},    Population = {userResponse[0].Population},    Region = {_check2} WHERE code = '{userResponse[0].Alpha3Code}'", _sqlConnection);
                    sqlCommand.ExecuteNonQuery();
                    sqlCommand.Dispose();
                }
                else
                {
                    check = GetValue(selectQuery, value);
                }
            }
          
            else
            {
                try
                {
                    if (trigger == true)// так же чтобы insert информацию, нужна отличающийся запрос от двух других таблиц
                    {
                        sqlCommand = new SqlCommand($"{insert}('{userResponse[0].Name}','{userResponse[0].Alpha3Code}',{_check1},{userResponse[0].Area}, {userResponse[0].Population},{_check2})", _sqlConnection);
                        sqlCommand.ExecuteNonQuery();
                        sqlCommand.Dispose();
                    }
                    else
                    {
                        sqlCommand = new SqlCommand($"{insert}('{value}')", _sqlConnection);
                        sqlCommand.ExecuteNonQuery();
                        sqlCommand.Dispose();

                        check = GetValue(selectQuery, value);
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Возможно другой пользователь внес данные в базу данных по такому же запросу. Ещё раз повторите. {ex.Message}");
                }
            }
           
            _sqlConnection.Close();
            return check;
        }

        private string GetValue(string line , string value)
        {
            string check = string.Empty;
            SqlCommand command = new SqlCommand($"{line}'{value}'", _sqlConnection);
            SqlDataReader srd = command.ExecuteReader();
            srd.Read();
            check = srd.GetValue(0).ToString();
            
            return check;
        }

        private (string,bool) InputCountry()
        {

            string URL = "https://restcountries.eu/rest/v2/name/" + textBox2.Text;
            HttpWebRequest httpWebRequest;
            HttpWebResponse httpWebResponse;
            StreamReader streamReader;
            string response;

            try
            {
                httpWebRequest = (HttpWebRequest)WebRequest.Create(URL);
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    response = streamReader.ReadToEnd();
                }
                return (response,true);
            }
            catch (UriFormatException ex)
            {
                MessageBox.Show($"{ex.Message}");
                return (string.Empty, false);
            }
            catch (WebException ex)
            {
                MessageBox.Show($"{ex.Message}");
                return (string.Empty, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось записать {ex.Message}");
                return (string.Empty,false);
            }


        }


        private void button3_Click(object sender, EventArgs e)
        {
            
            _sqlConnection.Open();
            SqlDataAdapter dataAdapter = new SqlDataAdapter
                ("select " +
                "c.name  Country_name, c.code Code, ct.name Capital, c.Area Area, c.Population population, r.name Region  " +
                "from country c " +
                "join city ct on c.capital=ct.id " +
                "join region r on c.region=r.id", _sqlConnection);
            DataSet dataSet = new DataSet();
            dataAdapter.Fill(dataSet);
            dataGridView1.DataSource = dataSet.Tables[0];
            _sqlConnection.Close();
        }

        private void textBox2_MouseClick(object sender, MouseEventArgs e)
        {
            textBox2.Text = string.Empty;
        }

        private void textBox1_MouseClick(object sender, MouseEventArgs e)
        {
            textBox1.Text = string.Empty;
        }
    }
}

