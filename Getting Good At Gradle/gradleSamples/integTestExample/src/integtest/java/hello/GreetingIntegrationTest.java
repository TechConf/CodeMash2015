package hello;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.Test;
import org.springframework.boot.test.TestRestTemplate;
import org.springframework.web.client.RestTemplate;

import static org.junit.Assert.assertEquals;

public class GreetingIntegrationTest {

    RestTemplate template = new TestRestTemplate();

    @Test
    public void testRequest() throws Exception {
        String response = template.getForEntity("http://localhost:8080/greeting?name=Bobby", String.class).getBody();
        ObjectMapper objectMapper = new ObjectMapper();
        JsonNode responseJson = objectMapper.readTree(response);
        JsonNode contentJson = responseJson.path("content");

        assertEquals("Test that response content is correct.", contentJson.textValue(), "Hello, Bobby!");
    }



}