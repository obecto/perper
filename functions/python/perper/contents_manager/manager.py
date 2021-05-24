from datetime import datetime
import errno
import io
import os
import shutil
import stat
import sys
import warnings
import mimetypes
import nbformat

from send2trash import send2trash
from send2trash.exceptions import TrashPermissionError
from tornado import web

from notebook.services.contents.filecheckpoints import FileCheckpoints
from notebook.services.contents.fileio import FileManagerMixin
from notebook.services.contents.manager import ContentsManager
from notebook.utils import exists

from ipython_genutils.importstring import import_item
from traitlets import Any, Unicode, Bool, TraitError, observe, default, validate
from ipython_genutils.py3compat import getcwd, string_types

from notebook import _tz as tz
from notebook.utils import (
    is_hidden, is_file_hidden,
    to_api_path,
)
from notebook.base.handlers import AuthenticatedFileHandler
from notebook.transutils import _

from os.path import samefile
_script_exporter = None

import json
import asyncio
import threading
from perper.functions import Perper

os.environ["PERPER_AGENT_NAME"] = "python-functions"
FINAL_ID = 10
perper = Perper()

def _post_save_script(model, os_path, contents_manager, **kwargs):
    from nbconvert.exporters.script import ScriptExporter
    warnings.warn("`_post_save_script` is deprecated and will be removed in Notebook 5.0", DeprecationWarning)

    if model['type'] != 'notebook':
        return

    global _script_exporter
    if _script_exporter is None:
        _script_exporter = ScriptExporter(parent=contents_manager)
    log = contents_manager.log

    base, ext = os.path.splitext(os_path)
    script, resources = _script_exporter.from_filename(os_path)
    script_fname = base + resources.get('output_extension', '.txt')
    log.info("Saving script /%s", to_api_path(script_fname, contents_manager.root_dir))
    with io.open(script_fname, 'w', encoding='utf-8') as f:
        f.write(script)


class PerperManager(FileManagerMixin, ContentsManager):

    root_dir = Unicode(config=True)
    is_running = False
    current_data = ''

    @default('root_dir')
    def _default_root_dir(self):
        try:
            return self.parent.notebook_dir
        except AttributeError:
            return getcwd()

    save_script = Bool(False, config=True, help='DEPRECATED, use post_save_hook. Will be removed in Notebook 5.0')
    @observe('save_script')
    def _update_save_script(self, change):
        if not change['new']:
            return

        self.post_save_hook = _post_save_script

    post_save_hook = Any(None, config=True, allow_none=True, help="")

    @validate('post_save_hook')
    def _validate_post_save_hook(self, proposal):
        value = proposal['value']
        if isinstance(value, string_types):
            value = import_item(value)
        if not callable(value):
            raise TraitError("post_save_hook must be callable")
        return value

    def run_post_save_hook(self, model, os_path):
        if self.post_save_hook:
            try:
                self.log.debug("Running post-save hook on %s", os_path)
                self.post_save_hook(os_path=os_path, model=model, contents_manager=self)
            except Exception as e:
                self.log.error("Post-save hook failed o-n %s", os_path, exc_info=True)
                raise web.HTTPError(500, u'Unexpected error while running post hook save: %s'
                                    % e) from e

    @validate('root_dir')
    def _validate_root_dir(self, proposal):
        value = proposal['value']
        if not os.path.isabs(value):
            # If we receive a non-absolute path, make it absolute.
            value = os.path.abspath(value)
        if not os.path.isdir(value):
            raise TraitError("%r is not a directory" % value)
        return value

    @default('checkpoints_class')
    def _checkpoints_class_default(self):
        return FileCheckpoints

    delete_to_trash = Bool(True, config=True, help="")

    @default('files_handler_class')
    def _files_handler_class_default(self):
        return AuthenticatedFileHandler

    @default('files_handler_params')
    def _files_handler_params_default(self):
        return {'path': self.root_dir}

    def is_hidden(self, path):
        path = path.strip('/')
        os_path = self._get_os_path(path=path)
        return is_hidden(os_path, self.root_dir)

    def file_exists(self, path):
        path = path.strip('/')
        os_path = self._get_os_path(path)
        return os.path.isfile(os_path)

    def dir_exists(self, path):
        path = path.strip('/')
        os_path = self._get_os_path(path=path)
        return os.path.isdir(os_path)

    def exists(self, path):
        path = path.strip('/')
        os_path = self._get_os_path(path=path)
        return exists(os_path)

    def _base_model(self, path):
        os_path = self._get_os_path(path)
        info = os.lstat(os_path)
        
        try:
            # size of file 
            size = info.st_size
        except (ValueError, OSError):
            self.log.warning('Unable to get size.')
            size = None
        
        try:
            last_modified = tz.utcfromtimestamp(info.st_mtime)
        except (ValueError, OSError):
            # Files can rarely have an invalid timestamp
            # https://github.com/jupyter/notebook/issues/2539
            # https://github.com/jupyter/notebook/issues/2757
            # Use the Unix epoch as a fallback so we don't crash.
            self.log.warning('Invalid mtime %s for %s', info.st_mtime, os_path)
            last_modified = datetime(1970, 1, 1, 0, 0, tzinfo=tz.UTC)

        try:
            created = tz.utcfromtimestamp(info.st_ctime)
        except (ValueError, OSError):  # See above
            self.log.warning('Invalid ctime %s for %s', info.st_ctime, os_path)
            created = datetime(1970, 1, 1, 0, 0, tzinfo=tz.UTC)

        # Create the base model.
        model = {}
        model['name'] = path.rsplit('/', 1)[-1]
        model['path'] = path
        model['last_modified'] = last_modified
        model['created'] = created
        model['content'] = None
        model['format'] = None
        model['mimetype'] = None
        model['size'] = size

        try:
            model['writable'] = os.access(os_path, os.W_OK)
        except OSError:
            self.log.error("Failed to check write permissions on %s", os_path)
            model['writable'] = False
        return model

    def _dir_model(self, path, content=True):
        os_path = self._get_os_path(path)

        four_o_four = u'directory does not exist: %r' % path

        if not os.path.isdir(os_path):
            raise web.HTTPError(404, four_o_four)
        elif is_hidden(os_path, self.root_dir) and not self.allow_hidden:
            self.log.info("Refusing to serve hidden directory %r, via 404 Error",
                os_path
            )
            raise web.HTTPError(404, four_o_four)

        model = self._base_model(path)
        model['type'] = 'directory'
        model['size'] = None
        if content:
            model['content'] = contents = []
            os_dir = self._get_os_path(path)
            for name in os.listdir(os_dir):
                try:
                    os_path = os.path.join(os_dir, name)
                except UnicodeDecodeError as e:
                    self.log.warning(
                        "failed to decode filename '%s': %s", name, e)
                    continue

                try:
                    st = os.lstat(os_path)
                except OSError as e:
                    # skip over broken symlinks in listing
                    if e.errno == errno.ENOENT:
                        self.log.warning("%s doesn't exist", os_path)
                    else:
                        self.log.warning("Error stat-ing %s: %s", os_path, e)
                    continue

                if (not stat.S_ISLNK(st.st_mode)
                        and not stat.S_ISREG(st.st_mode)
                        and not stat.S_ISDIR(st.st_mode)):
                    self.log.debug("%s not a regular file", os_path)
                    continue

                try:
                    if self.should_list(name):
                        if self.allow_hidden or not is_file_hidden(os_path, stat_res=st):
                            contents.append(
                                    self.get(path='%s/%s' % (path, name), content=False)
                            )
                except OSError as e:
                    # ELOOP: recursive symlink
                    if e.errno != errno.ELOOP:
                        self.log.warning(
                            "Unknown error checking if file %r is hidden",
                            os_path,
                            exc_info=True,
                        )
            model['format'] = 'json'

        return model


    def _file_model(self, path, content=True, format=None):
        model = self._base_model(path)
        model['type'] = 'file'

        os_path = self._get_os_path(path)
        model['mimetype'] = mimetypes.guess_type(os_path)[0]

        if content:
            content, format = self._read_file(os_path, format)
            if model['mimetype'] is None:
                default_mime = {
                    'text': 'text/plain',
                    'base64': 'application/octet-stream'
                }[format]
                model['mimetype'] = default_mime

            model.update(
                content=content,
                format=format,
            )

        return model

    def _notebook_model(self, path, content=True):
        model = self._base_model(path)
        model['type'] = 'notebook'
        os_path = self._get_os_path(path)
        
        if content:
            nb = self._read_notebook(os_path, as_version=4)
            self.mark_trusted_cells(nb, path)
            model['content'] = nb
            model['format'] = 'json'
            self.validate_notebook_model(model)
            
        return model

    async def afun(self, content):
        result = ''
        result = result + 'id,price\n' #TODO: UPDATE
        async for (k, n) in perper.fs.get_notifications():
            incoming_type = n.__class__.__name__
            if incoming_type == 'StreamItemNotification':
                cache = perper.ignite.get_cache(n.cache)
                item_data = cache.get(n.key)
                if item_data is not None:
                    # result.append(json.loads(item_data.json))
                    json_obj = json.loads(item_data.json)
                    result = result + str(json_obj['id']) + ',' + str(json_obj['price']) + '\n'
                    perper.fs.consume_notification(k)
                    if json_obj['id'] == FINAL_ID:
                        f = open('output.csv', 'w')
                        f.write(result)
                        f.close()
                        return

    def fire_and_forget(self, loop, content):
        if not self.is_running:
            self.is_running = True
            asyncio.set_event_loop(loop)
            loop.run_until_complete(self.afun(content))
            self.is_running = False
            print('Async function finished!')
        else:
            print('RUNNING!')

    def get(self, path, content=True, type=None, format=None):
        path = path.strip('/')

        if not self.exists(path):
            raise web.HTTPError(404, u'No such file or directory: %s' % path)


        os_path = self._get_os_path(path)
        if os.path.isdir(os_path):
            if type not in (None, 'directory'):
                raise web.HTTPError(400,
                                u'%s is a directory, not a %s' % (path, type), reason='bad type')
            model = self._dir_model(path, content=content)
        elif type == 'notebook' or (type is None and path.endswith('.ipynb')):
            model = self._notebook_model(path, content=content)
        else:
            if type == 'directory':
                raise web.HTTPError(400,
                                u'%s is not a directory' % path, reason='bad type')
            model = self._file_model(path, content=content, format=format)

        return model

    def _save_directory(self, os_path, model, path=''):
        if is_hidden(os_path, self.root_dir) and not self.allow_hidden:
            raise web.HTTPError(400, u'Cannot create hidden directory %r' % os_path)
        if not os.path.exists(os_path):
            with self.perm_to_403():
                os.mkdir(os_path)
        elif not os.path.isdir(os_path):
            raise web.HTTPError(400, u'Not a directory: %s' % (os_path))
        else:
            self.log.debug("Directory %r already exists", os_path)

    def save(self, model, path=''):
        path = path.strip('/')


        if 'type' not in model:
            raise web.HTTPError(400, u'No file type provided')
        if 'content' not in model and model['type'] != 'directory':
            raise web.HTTPError(400, u'No file content provided')

        os_path = self._get_os_path(path)
        self.log.debug("Saving %s", os_path)

        self.run_pre_save_hook(model=model, path=path)

        try:
            if model['type'] == 'notebook':
                print('NOTEBOOK') # TODO: Custom handling
                nb = nbformat.from_dict(model['content'])
                self.check_and_sign(nb, path)
                self._save_notebook(os_path, nb)
                # One checkpoint should always exist for notebooks.
                if not self.checkpoints.list_checkpoints(path):
                    self.create_checkpoint(path)
            elif model['type'] == 'file':
                if model['path'][-5:] == '.json':
                    loop = asyncio.new_event_loop()
                    thread = threading.Thread(target=self.fire_and_forget, args=(loop,model['content']))
                    thread.start()

                # Missing format will be handled internally by _save_file.
                self._save_file(os_path, model['content'], model.get('format'))
            elif model['type'] == 'directory':
                self._save_directory(os_path, model, path)
            else:
                raise web.HTTPError(400, "Unhandled contents type: %s" % model['type'])
        except web.HTTPError:
            raise
        except Exception as e:
            self.log.error(u'Error while saving file: %s %s', path, e, exc_info=True)
            raise web.HTTPError(500, u'Unexpected error while saving file: %s %s' %
                                (path, e)) from e

        validation_message = None
        if model['type'] == 'notebook':
            self.validate_notebook_model(model)
            validation_message = model.get('message', None)

        model = self.get(path, content=False)
        if validation_message:
            model['message'] = validation_message

        self.run_post_save_hook(model=model, os_path=os_path)

        return model

    def delete_file(self, path):
        path = path.strip('/')
        os_path = self._get_os_path(path)
        rm = os.unlink
        if not os.path.exists(os_path):
            raise web.HTTPError(404, u'File or directory does not exist: %s' % os_path)

        def is_non_empty_dir(os_path):
            if os.path.isdir(os_path):
                # A directory containing only leftover checkpoints is
                # considered empty.
                cp_dir = getattr(self.checkpoints, 'checkpoint_dir', None)
                if set(os.listdir(os_path)) - {cp_dir}:
                    return True

            return False

        if self.delete_to_trash:
            if sys.platform == 'win32' and is_non_empty_dir(os_path):
                # send2trash can really delete files on Windows, so disallow
                # deleting non-empty files. See Github issue 3631.
                raise web.HTTPError(400, u'Directory %s not empty' % os_path)
            try:
                self.log.debug("Sending %s to trash", os_path)
                send2trash(os_path)
                return
            except TrashPermissionError as e:
                self.log.warning("Skipping trash for %s, %s", os_path, e)

        if os.path.isdir(os_path):
            # Don't permanently delete non-empty directories.
            if is_non_empty_dir(os_path):
                raise web.HTTPError(400, u'Directory %s not empty' % os_path)
            self.log.debug("Removing directory %s", os_path)
            with self.perm_to_403():
                shutil.rmtree(os_path)
        else:
            self.log.debug("Unlinking file %s", os_path)
            with self.perm_to_403():
                rm(os_path)

    def rename_file(self, old_path, new_path):
        old_path = old_path.strip('/')
        new_path = new_path.strip('/')
        if new_path == old_path:
            return

        # Perform path validation prior to converting to os-specific value since this
        # is still relative to root_dir.
        self._validate_path(new_path)

        new_os_path = self._get_os_path(new_path)
        old_os_path = self._get_os_path(old_path)

        # Should we proceed with the move?
        if os.path.exists(new_os_path) and not samefile(old_os_path, new_os_path):
            raise web.HTTPError(409, u'File already exists: %s' % new_path)

        # Move the file
        try:
            with self.perm_to_403():
                shutil.move(old_os_path, new_os_path)
        except web.HTTPError:
            raise
        except Exception as e:
            raise web.HTTPError(500, u'Unknown error renaming file: %s %s' %
                                (old_path, e)) from e

    def info_string(self):
        return _("Serving notebooks from local directory: %s") % self.root_dir

    def get_kernel_path(self, path, model=None):
        if self.dir_exists(path):
            return path
        if '/' in path:
            parent_dir = path.rsplit('/', 1)[0]
        else:
            parent_dir = ''
        return parent_dir

    @staticmethod
    def _validate_path(path):
        if sys.platform == 'win32':
            # On Windows systems, we MUST disallow colons otherwise an Alternative Data Stream will
            # be created and confusion will reign! (See https://github.com/jupyter/notebook/issues/5190)
            # Go ahead and add other invalid (and non-path-separator) characters here as well so there's
            # consistent behavior - although all others will result in '[Errno 22]Invalid Argument' errors.
            invalid_chars = '?:><*"|'
        else:
            # On non-windows systems, allow the underlying file creation to perform enforcement when appropriate
            invalid_chars = ''

        for char in invalid_chars:
            if char in path:
                raise web.HTTPError(400, "Path '{}' contains characters that are invalid for the filesystem. "
                                         "Path names on this filesystem cannot contain any of the following "
                                         "characters: {}".format(path, invalid_chars))