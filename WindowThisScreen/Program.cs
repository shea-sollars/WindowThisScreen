using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Vanara.PInvoke;

class Program {
    static User32.HWINEVENTHOOK hook;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetParent(IntPtr hWnd);  // Import GetParent from user32.dll

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    static void Main(string[] args) {
        if (args.Length > 0 && args[0] == "--hidden") {
            var handle = Kernel32.GetConsoleWindow();
            User32.ShowWindow(handle, SW_HIDE);  // Hide the console window
        }

        hook = User32.SetWinEventHook(User32.EventConstants.EVENT_OBJECT_SHOW, User32.EventConstants.EVENT_OBJECT_SHOW, IntPtr.Zero, WinEventProc, 0, 0, User32.WINEVENT.WINEVENT_OUTOFCONTEXT);

        Console.WriteLine("Monitoring new window creation...");
        Application.Run();

        User32.UnhookWinEvent(hook);
    }

    // Function to check if a window is a child window
    private static bool IsChildWindow(HWND hwnd) {

        IntPtr parentHandle = GetParent(hwnd.DangerousGetHandle());
        return parentHandle != IntPtr.Zero;  // If it has a parent, it is a child window
    }

    // Helper function to determine if the window is a WPF window with HwndWrapper
    private static bool IsWpfWindow(string className) {
        return className.StartsWith("HwndWrapper");
    }

    // Callback function for handling window events
    private static void WinEventProc(User32.HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
        string className = GetWindowClassName(hwnd);
        //Console.WriteLine($"\nclassName: {className}");

        // Skip child windows by checking if the window has a parent
        if (IsChildWindow(hwnd)) {
            Console.WriteLine($"Skipping child window: {className}");
            return;
        }

        if (IsWpfWindow(className)) {
            Console.WriteLine($"Skipping WPF window: {className}");
            return;
        }

        if (className.StartsWith("ApplicationFrameWindow")) {
            // Console.WriteLine($"Detected UWP app window: {className}");

            // Delay moving the entire UWP app to allow for the final position to be set
            Timer timer = new Timer();
            timer.Interval = 300;  // 300ms delay
            timer.Tick += (sender, e) => {
                MoveWindowToMouseScreen(hwnd);
                timer.Stop();  // Stop the timer after it fires
            };
            timer.Start();
        } else {
            MoveWindowToMouseScreen(hwnd);  // Handle regular windows
        }
    }

    // Function to move regular windows to the screen where the mouse is located
    private static void MoveWindowToMouseScreen(HWND hwnd) {
        RECT windowRect;

        if (User32.GetWindowRect(hwnd, out windowRect)) {
            MoveWindowToTargetScreen(hwnd, windowRect);
        } else {
            Console.WriteLine("Failed to get window rectangle for Win32 app.");
        }
    }

    // Helper function to move window to the target screen
    private static void MoveWindowToTargetScreen(HWND hwnd, RECT windowRect) {
        // Find the screen where the mouse is located
        var targetScreen = Screen.FromPoint(Cursor.Position);

        // Check if the window is already on the target screen
        if (IsWindowOnScreen(windowRect, targetScreen)) {
            string className = GetWindowClassName(hwnd);
            Console.WriteLine($"Window {className} is already on the target screen");
            return;
        }

        // Find the screen where the window is currently located
        var currentScreen = Screen.FromRectangle(new System.Drawing.Rectangle(windowRect.left, windowRect.top, windowRect.Width, windowRect.Height));

        // Calculate new window position
        var newPos = CalculateNewPosition(windowRect, currentScreen, targetScreen);

        // Use SetWindowPos with specific flags for UWP windows
        int width = windowRect.Width;
        int height = windowRect.Height;

        // Attempt to move the window using more robust flags for handling UWP
        bool moved = User32.SetWindowPos(hwnd, HWND.NULL, newPos.X, newPos.Y, width, height, User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_NOACTIVATE | User32.SetWindowPosFlags.SWP_NOOWNERZORDER | User32.SetWindowPosFlags.SWP_FRAMECHANGED);

        if (!moved) {
            Console.WriteLine($"Failed to move window. Error code: {Marshal.GetLastWin32Error()}");
        } else {
            string className = GetWindowClassName(hwnd);
            Console.WriteLine($"Moved window {className} to screen {targetScreen.DeviceName}");
        }
    }

    // Helper function to calculate the new position of the window
    private static System.Drawing.Point CalculateNewPosition(RECT windowRect, Screen currentScreen, Screen targetScreen) {
        // Calculate the scaling factor between the two screens
        float scaleX = (float) targetScreen.Bounds.Width / currentScreen.Bounds.Width;
        float scaleY = (float) targetScreen.Bounds.Height / currentScreen.Bounds.Height;

        // Calculate the new position based on the scaling factor
        int newX = targetScreen.WorkingArea.X + (int) ((windowRect.X - currentScreen.WorkingArea.X) * scaleX);
        int newY = targetScreen.WorkingArea.Y + (int) ((windowRect.Y - currentScreen.WorkingArea.Y) * scaleY);

        return new System.Drawing.Point(newX, newY);
    }

    // Helper function to check if the window is already on the target screen
    private static bool IsWindowOnScreen(RECT windowRect, Screen screen) {
        return screen.WorkingArea.Contains(windowRect.X, windowRect.Y);
    }

    // Helper function to get the window class name
    private static string GetWindowClassName(HWND hwnd) {
        const int MaxClassNameLength = 256;
        StringBuilder className = new StringBuilder(MaxClassNameLength);
        User32.GetClassName(hwnd, className, MaxClassNameLength);
        return className.ToString();
    }
}
