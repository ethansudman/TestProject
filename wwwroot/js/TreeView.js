function loadTreeView() {
    console.log("Loading data");

    // In the future, we probably want to use a more robust way to determine the URL of the server, but for now we'll just hardcode it.
    fetch('https://localhost:7146/DirectoryBrowsing/directories')
        .then(response => response.json())
        .then(json => {
            console.log('json', json);
            const insertionpoint = document.getElementById('directorytree');
            // Clear any existing contents
            insertionpoint.innerHTML = '';
            // Render the root entity (the JSON provided)
            renderEntity(json, insertionpoint);
            attachCaretHandlers();
        })
        .catch(err => console.error('Failed to load directory tree', err));
}

async function saveFileToDisk(blob, filename) {
    console.log('File name', filename)
    // Prefer File System Access API when available (user chooses where to save)
    if (window.showSaveFilePicker) {
        try {
            //const opts = { suggestedName: filename, types: [{ description: 'All Files', accept: { '*/*': ['.'] } }] };
            const handle = await window.showSaveFilePicker({
                suggestedName: filename
            });
            const writable = await handle.createWritable();
            await writable.write(blob);
            await writable.close();
        } catch (err) {
            // user likely cancelled or permission denied -> fall through to download link
            console.warn('Save via File System Access API failed or cancelled, falling back to download link', err);
        }
    }
    else {
        // Fallback: create a temporary link and click it to download
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        // Append to document to make click work in some browsers
        document.body.appendChild(a);
        a.click();
        a.remove();
        // Release memory
        URL.revokeObjectURL(url);
    }
}

// Recursively render an entity into a parent <ul>
function renderEntity(entity, parentUl) {
    if (!entity) return;

    // Derive display name from path
    const name = (entity.path || '').split('\\').pop() || entity.entityType;

    // For folders: create a caret span and nested <ul>
    if (entity.entityType === 'Folder') {
        const li = document.createElement('li');

        const span = document.createElement('span');
        span.className = 'caret';
        span.textContent = `${name} (${entity.entityCount} entities, total size ${entity.size} bytes)`;
        li.appendChild(span);

        const nested = document.createElement('ul');
        nested.className = 'nested';
        // Render children
        if (Array.isArray(entity.subentities)) {
            entity.subentities.forEach(child => renderEntity(child, nested));
        }
        li.appendChild(nested);
        parentUl.appendChild(li);
    } else { // File or unknown -> render as plain list item
        const li = document.createElement('li');
        const fileSpan = document.createElement('span');
        fileSpan.className = 'file';
        fileSpan.textContent = `${name} (${entity.size} bytes)`;
        // store full path for later use if needed
        fileSpan.dataset.path = entity.path || '';
        // We want users to be able to click on files to download them
        fileSpan.onclick = function () {
            const path = this.dataset.path;
            console.log('Clicked file with path:', path);
            fetch(`https://localhost:7146/DirectoryBrowsing/download?path=${encodeURIComponent(path)}`)
                .then(response => response.blob())
                .then(blob => saveFileToDisk(blob, name))
                .catch(err => console.error('Failed to download file', err));
        }
        li.appendChild(fileSpan);
        parentUl.appendChild(li);
    }
}

// Attach click handlers to caret elements to toggle nested lists
function attachCaretHandlers() {
    const togglers = document.getElementsByClassName('caret');
    for (let i = 0; i < togglers.length; i++) {
        const t = togglers[i];
        // avoid adding duplicate handlers
        if (t._hasClick) continue;
        t._hasClick = true;
        t.addEventListener('click', function () {
            const nested = this.nextElementSibling;
            if (nested) nested.classList.toggle('active');
            this.classList.toggle('caret-down');
        });
    }
}