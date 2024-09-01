using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FaceApiSample
{
    public partial class MainWindow : Window
    {
        private static readonly string endpoint = "";
        private static readonly string subscriptionKey = "";
        private static readonly string personGroupId = "";

        private HttpClient httpClient;
        private List<string> pictureUrls; // 用于存储图片 URL
        private string currentImageUrl; // 用于存储当前预览的图像 URL

        public MainWindow()
        {
            InitializeComponent();
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            pictureUrls = new List<string>(); // 初始化图片 URL 列表
            InitializePersonGroupId(); // 确保 personGroupId 被正确设置
        }

        private void InitializePersonGroupId()
        {
            // 确保 personGroupId 被正确设置
            if (string.IsNullOrWhiteSpace(personGroupId))
            {
                MessageBox.Show("Person group ID is not set. Please ensure the person group is properly created.");
            }
        }

        private async void AddPersonButton_Click(object sender, RoutedEventArgs e)
        {
            string name = nameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a name.");
                return;
            }

            try
            {
                var uri = $"{endpoint}/face/v1.2-preview.1/persongroups/{personGroupId}/persons";
                var content = new StringContent($"{{\"name\":\"{name}\"}}", System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(uri, content);
                response.EnsureSuccessStatusCode(); // 确保响应成功
                outputTextBox.Text = $"Person '{name}' added successfully.";
            }
            catch (Exception ex)
            {
                outputTextBox.Text = $"Error adding person: {ex.Message}";
            }
        }

        private async void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            string url = urlTextBox.Text.Trim();
            string name = nameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter both URL and name.");
                return;
            }

            try
            {
                // Get list of persons and find the person with the given name
                var uri = $"{endpoint}/face/v1.2-preview.1/persongroups/{personGroupId}/persons";
                var response = await httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode(); // 确保响应成功
                var responseBody = await response.Content.ReadAsStringAsync();
                var persons = JArray.Parse(responseBody);
                var personId = persons.FirstOrDefault(p => p["name"].ToString() == name)?["personId"]?.ToString();

                if (string.IsNullOrWhiteSpace(personId))
                {
                    MessageBox.Show("Person not found.");
                    return;
                }

                // Add photo to the person
                uri = $"{endpoint}/face/v1.2-preview.1/persongroups/{personGroupId}/persons/{personId}/persistedFaces";
                var photoContent = new StringContent($"{{\"url\":\"{url}\"}}", System.Text.Encoding.UTF8, "application/json");
                response = await httpClient.PostAsync(uri, photoContent);
                response.EnsureSuccessStatusCode(); // 确保响应成功

                // 将 URL 添加到图片列表
                pictureUrls.Add(url);

                outputTextBox.Text = "Photo added successfully.";
            }
            catch (Exception ex)
            {
                outputTextBox.Text = $"Error adding photo: {ex.Message}";
            }
        }

        private async void DetectButton_Click(object sender, RoutedEventArgs e)
        {
            string url = urlTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    // 更新 URI 以包括 detectionModel 和 recognitionModel 参数
                    var uri = $"{endpoint}/face/v1.0/detect?returnFaceAttributes=glasses,qualityForRecognition,blur&detectionModel=detection_03&recognitionModel=recognition_04";

                    // 请求体中的 JSON 结构
                    var requestBody = new
                    {
                        url = url
                    };

                    // 创建请求体
                    var content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");

                    // 发送请求
                    var response = await httpClient.PostAsync(uri, content);

                    // 确保响应成功
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var faces = JArray.Parse(jsonResponse);

                    // 显示检测结果
                    outputTextBox.Text = "Face Detection Results:\n";
                    foreach (var face in faces)
                    {
                        var attributes = face["faceAttributes"];
                        outputTextBox.Text += $"- Glasses: {attributes["glasses"]}\n";
                        outputTextBox.Text += $"- Quality For Recognition: {attributes["qualityForRecognition"]}\n";

                        var blur = attributes["blur"];
                        if (blur != null)
                        {
                            outputTextBox.Text += $"- Blur Level: {blur["blurLevel"]}\n";
                            outputTextBox.Text += $"- Blur Value: {blur["value"]}\n";
                        }
                        else
                        {
                            outputTextBox.Text += "- Blur: No data available\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    outputTextBox.Text = $"Error: {ex.Message}";
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid URL.");
            }
        }

        private async void TrainButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = $"{endpoint}/face/v1.2-preview.1/persongroups/{personGroupId}/train";
                var response = await httpClient.PostAsync(uri, null);
                response.EnsureSuccessStatusCode();

                outputTextBox.Text = "Training initiated. Please wait until the training is completed.";

                // 调用 CheckTrainingStatusAsync 并等待其完成
                await CheckTrainingStatusAsync();
            }
            catch (Exception ex)
            {
                outputTextBox.Text = $"Error initiating training: {ex.Message}";
            }
        }

        private async Task<string> CheckTrainingStatusAsync()
        {
            while (true)
            {
                try
                {
                    var uri = $"{endpoint}/face/v1.2-preview.1/persongroups/{personGroupId}/training";
                    var response = await httpClient.GetAsync(uri);
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JObject.Parse(responseBody);

                    var status = result["status"].ToString();

                    if (status == "succeeded")
                    {
                        outputTextBox.Text = "Training completed successfully.";
                        return "succeeded";
                    }
                    else if (status == "failed")
                    {
                        outputTextBox.Text = "Training failed. Please check the training status.";
                        return "failed";
                    }
                    else
                    {
                        // Training status is still in progress
                        outputTextBox.Text = $"Training status: {status}. Checking again in 10 seconds...";
                        await Task.Delay(10000); // Wait for 10 seconds before checking again
                    }
                }
                catch (Exception ex)
                {
                    outputTextBox.Text = $"Error checking training status: {ex.Message}";
                    return "error";
                }
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            string url = urlTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(url, UriKind.Absolute);
                    bitmap.EndInit();
                    photoImage.Source = bitmap;

                    // 保存当前预览的图像 URL
                    currentImageUrl = url;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid URL.");
            }
        }

        private void ShowPicturesButton_Click(object sender, RoutedEventArgs e)
        {
            var picturesWindow = new PicturesWindow(pictureUrls);
            picturesWindow.Show();
        }

        private async void IdentifyButton_Click(object sender, RoutedEventArgs e)
        {
            string url = currentImageUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("No image to identify. Please preview an image first.");
                return;
            }

            try
            {
                // 确保 personGroupId 被正确设置
                if (string.IsNullOrWhiteSpace(personGroupId))
                {
                    MessageBox.Show("Person group ID is not set. Please ensure the person group is properly created.");
                    return;
                }

                // 1. 确认训练已完成
                var trainingStatus = await CheckTrainingStatusAsync();
                if (trainingStatus != "succeeded")
                {
                    outputTextBox.Text = trainingStatus == "error" ? "Error occurred while checking training status." : "Model training is not completed. Please wait until training is finished.";
                    return;
                }

                // 2. 检测图片中的人脸，使用 detection_03 模型
                var detectUri = $"{endpoint}/face/v1.2-preview.1/detect?returnFaceIds=true&returnFaceLandmarks=false&detectionModel=detection_03";
                var detectRequestBody = new
                {
                    url = url
                };

                var detectContent = new StringContent(JsonConvert.SerializeObject(detectRequestBody), System.Text.Encoding.UTF8, "application/json");
                var detectResponse = await httpClient.PostAsync(detectUri, detectContent);

                if (!detectResponse.IsSuccessStatusCode)
                {
                    outputTextBox.Text = $"Error detecting faces: {await detectResponse.Content.ReadAsStringAsync()}";
                    return;
                }

                var detectResponseBody = await detectResponse.Content.ReadAsStringAsync();
                var detectedFaces = JArray.Parse(detectResponseBody);

                if (!detectedFaces.Any())
                {
                    outputTextBox.Text = "No faces detected in the image.";
                    return;
                }

                // 获取检测到的脸部 ID
                var faceIds = detectedFaces.Select(face => face["faceId"].ToString()).ToList();

                // 3. 执行识别操作，使用 recognition_04 模型
                var identifyUri = $"{endpoint}/face/v1.2-preview.1/identify";
                var identifyRequestBody = new
                {
                    personGroupId = personGroupId,
                    faceIds = faceIds,
                    maxNumOfCandidatesReturned = 1,
                    recognitionModel = "recognition_04"
                };

                var identifyContent = new StringContent(JsonConvert.SerializeObject(identifyRequestBody), System.Text.Encoding.UTF8, "application/json");
                var identifyResponse = await httpClient.PostAsync(identifyUri, identifyContent);

                if (!identifyResponse.IsSuccessStatusCode)
                {
                    outputTextBox.Text = $"Error identifying faces: {await identifyResponse.Content.ReadAsStringAsync()}";
                    return;
                }

                var identifyResponseBody = await identifyResponse.Content.ReadAsStringAsync();
                var identifyResults = JArray.Parse(identifyResponseBody);

                // 4. 显示识别结果
                outputTextBox.Text = "Identification Results:\n";
                foreach (var result in identifyResults)
                {
                    var faceId = result["faceId"].ToString();
                    var candidates = result["candidates"] as JArray;

                    if (candidates.Any())
                    {
                        var personId = candidates.First()["personId"].ToString();
                        var confidence = candidates.First()["confidence"].ToString();

                        // 获取该 personId 的姓名
                        var personUri = $"{endpoint}/face/v1.2-preview.1/persongroups/{personGroupId}/persons/{personId}";
                        var personResponse = await httpClient.GetAsync(personUri);

                        if (!personResponse.IsSuccessStatusCode)
                        {
                            outputTextBox.Text = $"Error fetching person details: {await personResponse.Content.ReadAsStringAsync()}";
                            return;
                        }

                        var personResponseBody = await personResponse.Content.ReadAsStringAsync();
                        var person = JObject.Parse(personResponseBody);

                        var personName = person["name"].ToString();
                        outputTextBox.Text += $"- FaceId: {faceId}\n";
                        outputTextBox.Text += $"- Person: {personName}\n";
                        outputTextBox.Text += $"- Confidence: {confidence}\n";
                    }
                    else
                    {
                        outputTextBox.Text += $"- FaceId: {faceId}\n";
                        outputTextBox.Text += "- No matching person found.\n";
                    }
                }
            }
            catch (Exception ex)
            {
                outputTextBox.Text = $"Error identifying face: {ex.Message}";
            }
        }
    }
}


















