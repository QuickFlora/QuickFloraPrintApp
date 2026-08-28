# Installing QuickFlora Print

This is the small program that prints your receipts, worksheets and card messages, and opens your cash
drawer. It sits quietly in the bottom-right corner of your screen next to the clock and starts on its
own every time the PC turns on.

Installing takes about three minutes. You do not need to be technical, but you **do** need to be able
to install programs on the PC (an administrator account). If a message asks for an administrator
password and you don't have it, stop and call whoever looks after your computers.

---

## Step 1 — Download it

Download `QuickFloraPrintSetup.exe` from the link your QuickFlora contact sent you.

Do this **on the till PC itself** — the one connected to the receipt printer. Installing it on a
different computer will not print anything.

## Step 2 — Windows will try to stop you. This is expected.

When you run the file, Windows shows a blue box saying **"Windows protected your PC"**.

**This does not mean anything is wrong with the file.** It means we have not yet bought the digital
certificate that tells Windows who we are. We know about it and it is being sorted.

To continue:

1. Click the small **More info** link in the blue box
2. Click the **Run anyway** button that appears

If you'd rather check the file is genuine first, see *"Is this file safe?"* at the bottom.

## Step 3 — Install

Click through the installer. You do not need to change any settings — the defaults are correct, and
the install location in particular must not be changed or receipts will not print.

If Windows asks *"Do you want to allow this app to make changes to your device?"*, click **Yes**.

## Step 4 — Set it up

The first time it opens, a setup screen asks you three short things:

**1. Activation code** — if your QuickFlora contact gave you one, type it in. If not, click past it.

**2. Your shop details** — Company, Division, Department and Terminal. Your QuickFlora contact will
give you these four values. **They must be exactly right, including capital letters.** If the Terminal
name is wrong the program will run happily and never print anything, because it will be watching the
wrong till.

**3. Your receipt printer** — pick it from the dropdown list. The list shows the printers this PC can
actually see, and it will try to guess your receipt printer for you. If your receipt printer is not in
the list, it has not been set up in Windows yet — do that first, then reopen this screen.

## Step 5 — Test before you finish

Before the **Finish** button will work, use the two test buttons:

- **Test print** — a receipt should come out of the receipt printer
- **Test cash drawer** — the drawer should pop open

If the test print does nothing, the printer in the dropdown is the wrong one. Go back and pick another.

> **Note on the cash drawer:** if your drawer does not open during the test, tell your QuickFlora
> contact rather than working around it. The drawer function has not yet been confirmed on real shop
> hardware, and yours may be the first to try it. It is genuinely useful for us to hear either way.

Then click **Finish**. The program moves down to the clock area and starts watching for orders.

---

## Everyday use

**You do not need to do anything.** It starts by itself when the PC turns on and prints orders as they
come through.

To see it, click the small arrow next to the clock in the bottom-right and look for the QuickFlora
icon. Double-click it to open the window and see what it is doing.

**Leave the PC on** during shop hours. If the PC is off or asleep, nothing prints.

## If receipts stop printing

Try these in order:

1. **Check the program is running.** Look for the icon by the clock. If it isn't there, open it from
   the Start Menu (search for *QuickFlora Print*).
2. **Check the printer is on**, has paper, and isn't showing an error light.
3. **Open the window and read it.** It shows what it last did and will say if something failed.
4. **Restart the PC.** The program restarts with it.

If it is still not printing, call your QuickFlora contact and tell them **which shop, which till, and
what the program's window says**. That last part saves a great deal of time.

## To remove it

Windows **Settings → Apps → QuickFlora Print → Uninstall**, or use *Uninstall QuickFlora Print* in the
Start Menu.

Your settings and any saved receipts are deliberately left on the PC, so reinstalling picks up exactly
where you left off.

---

## Is this file safe?

Yes, and here is how to prove it rather than take our word for it.

Every release publishes a **checksum** — a short code calculated from the file's contents. If even one
byte of the file were different, the code would be completely different. Compare it to the one on the
download page:

1. Open the folder where you downloaded the file
2. Click **File → Open Windows PowerShell**
3. Paste this and press Enter:

```powershell
Get-FileHash .\QuickFloraPrintSetup.exe -Algorithm MD5
```

If the code it prints matches the one on the download page, the file is exactly the one we published.

If it does **not** match, do not run it — delete it and tell your QuickFlora contact immediately.
