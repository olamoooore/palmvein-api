from flask import Flask, request, jsonify
from escpos.printer import Usb  # or Network depending on connection

app = Flask(__name__)

# Configure your printer connection here
# Example: USB printer
PRINTER_VENDOR_ID = 0x0471  # Replace with your printer's Vendor ID
PRINTER_PRODUCT_ID = 0x0055  # Replace with your printer's Product ID

@app.route('/print_barcode', methods=['POST'])
def print_barcode():
    data = request.get_json()
    barcode_data = data.get('barcode', '')

    if not barcode_data:
        return jsonify({"error": "No barcode data"}), 400

    try:
        p = Usb(PRINTER_VENDOR_ID, PRINTER_PRODUCT_ID)
        p.barcode(barcode_data, 'CODE39', width=2, height=100, pos='BELOW', font='A')
        p.cut()
        return jsonify({"status": "Printed successfully"})
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(port=5000)
    
#http://localhost:5000/print_barcode