package hello;

import org.junit.Test;

import static org.junit.Assert.*;

public class GreetingControllerTest {

    @Test
    public void testGreeting() throws Exception {
         GreetingController greetingController = new GreetingController();
        assertEquals("Test the greeting controller creates the correct greeting",
                greetingController.greeting("Bob").getContent(),
                "Hello, Bob!");
    }
}