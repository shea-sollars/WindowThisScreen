## WindowTheScreen Intro

I use two monitors, and I find it annoying that an application will open on the right screen when I clicked on it from the start menu in the left screen or vice versa. I just want the application to open on whatever screen I am currently looking at, and 90% of the time when I open an application, I am looking at the screen I opened the application from. So I made this C# application that moves newly opened Windows applications to the monitor that the mouse is on.

## Run the Application as a Scheduled Task

### Create a Scheduled Task:

1. Open **Task Scheduler**.
2. Click on **Create Task**.
3. Set the task to **Run only when the user is logged on** to ensure it runs in the user session.
4. Under the **Triggers** tab, create a trigger for **At log on**.
5. Under the **Actions** tab, set **Start a program** and browse to your executable (`.exe` file).
    - Enter `--hidden` for `Add arguments (optional):` so that the console window will not be visible.
6. Under the **General** tab, check **Run with highest privileges**.

### Test the Task:

1. Run the task manually from Task Scheduler and see if it correctly moves windows as expected.
