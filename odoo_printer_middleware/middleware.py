from flask import Flask, request, jsonify
import win32print

app = Flask(__name__)

# Set your printer name exactly as in Windows
PRINTER_NAME = "Gprinter GP-D320FX"

def print_to_gprinter(text):
    hPrinter = win32print.OpenPrinter(PRINTER_NAME)
    try:
        hJob = win32print.StartDocPrinter(hPrinter, 1, ("Python Print Job", None, "RAW"))
        win32print.StartPagePrinter(hPrinter)
        win32print.WritePrinter(hPrinter, text.encode('utf-8'))
        win32print.EndPagePrinter(hPrinter)
        win32print.EndDocPrinter(hPrinter)
    finally:
        win32print.ClosePrinter(hPrinter)

@app.route("/print", methods=["POST"])
def print_label():
    data = request.get_json()
    if not data or "text" not in data:
        return jsonify({"error": "Missing 'text' in request"}), 400

    try:
        print_to_gprinter(data["text"])
        return jsonify({"status": "success", "message": "Print job sent"})
    except Exception as e:
        return jsonify({"status": "error", "message": str(e)}), 500

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)
