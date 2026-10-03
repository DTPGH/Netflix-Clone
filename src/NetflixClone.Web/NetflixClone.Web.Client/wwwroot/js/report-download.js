export async function downloadExcel(filename, stream) {
    const bytes = await stream.arrayBuffer();
    const url = URL.createObjectURL(new Blob([bytes], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }));
    const link = document.createElement("a");
    try {
        link.href = url; link.download = filename;
        document.body.appendChild(link); link.click();
    } finally {
        link.remove();
        // Give the browser time to begin downloading before releasing the object URL.
        setTimeout(() => URL.revokeObjectURL(url), 10000);
    }
}
